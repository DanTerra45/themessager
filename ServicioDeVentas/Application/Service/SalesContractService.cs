using System.Data;
using System.Globalization;
using System.Text.Json;
using Application.Sagas;
using Dapper;
using Domain.Common;
using Domain.Database;
using Domain.Events;
using Infrastructure.Integrations;
using ServicioVentas.Contracts.Sales;

namespace Application.Service;

public sealed class SalesContractService(
    IDbConnectionFactory dbConnectionFactory,
    CatalogApiClient catalogApiClient,
    IStockSagaCoordinator stockSagaCoordinator,
    IEventPublisher eventPublisher,
    ILogger<SalesContractService> logger)
{
    private const string RegisteredState = "Confirmed";
    private const string CancelledState = "Cancelled";
    private const string DefaultChannel = "Mostrador";
    private const string DefaultPaymentMethod = "Efectivo";

    public async Task<Result<SalesRegistrationContextResponse>> GetRegistrationContextAsync(
        string customerSearchTerm = "",
        string productSearchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var nextSaleCodeResult = await GetNextSaleCodeAsync(cancellationToken);
        if (nextSaleCodeResult.IsFailure)
        {
            return Result<SalesRegistrationContextResponse>.Failure(nextSaleCodeResult.Errors);
        }

        var customersResult = await SearchCustomersAsync(customerSearchTerm, cancellationToken);
        if (customersResult.IsFailure)
        {
            return Result<SalesRegistrationContextResponse>.Failure(customersResult.Errors);
        }

        var productsResult = await SearchProductsAsync(productSearchTerm, cancellationToken);
        if (productsResult.IsFailure)
        {
            return Result<SalesRegistrationContextResponse>.Failure(productsResult.Errors);
        }

        return Result<SalesRegistrationContextResponse>.Success(new SalesRegistrationContextResponse(
            nextSaleCodeResult.Value,
            customersResult.Value,
            productsResult.Value));
    }

    public async Task<Result<IReadOnlyList<CustomerOptionResponse>>> SearchCustomersAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        const string query = """
            SELECT
                id::bigint AS Id,
                COALESCE(
                    ci_nit,
                    CASE
                        WHEN complement IS NULL OR BTRIM(complement) = '' THEN ci::text
                        ELSE CONCAT(ci::text, '-', complement)
                    END,
                    '0') AS CiNit,
                razon_social AS BusinessName
            FROM customer
            WHERE @SearchTerm = ''
               OR COALESCE(
                    ci_nit,
                    CASE
                        WHEN complement IS NULL OR BTRIM(complement) = '' THEN ci::text
                        ELSE CONCAT(ci::text, '-', complement)
                    END,
                    '0') ILIKE @LikeSearchTerm
               OR razon_social ILIKE @LikeSearchTerm
            ORDER BY CASE WHEN id = 0 THEN 0 ELSE 1 END, razon_social ASC, id ASC
            LIMIT 20;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var rows = await connection.QueryAsync<CustomerOptionResponse>(
                new CommandDefinition(
                    query,
                    new
                    {
                        SearchTerm = searchTerm?.Trim() ?? string.Empty,
                        LikeSearchTerm = BuildLikeSearchValue(searchTerm)
                    },
                    cancellationToken: cancellationToken));

            return Result<IReadOnlyList<CustomerOptionResponse>>.Success(rows.AsList());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudieron consultar los clientes de ventas.");
            return Result<IReadOnlyList<CustomerOptionResponse>>.Internal("CustomerSearchFailed", "No se pudieron consultar los clientes de ventas.");
        }
    }

    public Task<Result<IReadOnlyList<SaleProductOptionResponse>>> SearchProductsAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default) =>
        catalogApiClient.SearchProductsAsync(searchTerm, cancellationToken);

    public async Task<Result<IReadOnlyList<SaleSummaryResponse>>> GetRecentSalesAsync(
        int take = 20,
        string sortBy = "createdat",
        string sortDirection = "desc",
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string status = "",
        string paymentMethod = "",
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return Result<IReadOnlyList<SaleSummaryResponse>>.Validation("fromDate", "InvalidDateRange", "La fecha inicial no puede ser mayor a la fecha final.");
        }

        var orderByClause = BuildRecentSalesOrderBy(sortBy, sortDirection);
        var normalizedStatus = NormalizeStatusFilter(status);
        var startDate = fromDate?.ToDateTime(TimeOnly.MinValue);
        var endDateExclusive = toDate?.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var query = $"""
            SELECT
                s.id AS Id,
                COALESCE(s.code, @FallbackCodePrefix || LPAD(s.id::text, 6, '0')) AS Code,
                s.created_at AS CreatedAt,
                c.razon_social AS CustomerName,
                COALESCE(s.channel, @DefaultChannel) AS Channel,
                COALESCE(s.payment_method, @DefaultPaymentMethod) AS PaymentMethod,
                s.total_price AS Total,
                CASE
                    WHEN s.state::text = @CancelledState THEN 'Anulada'
                    ELSE 'Registrada'
                END AS Status
            FROM sales s
            INNER JOIN customer c ON c.id = s.customer_id
            WHERE (@StartDate IS NULL OR s.created_at >= @StartDate)
              AND (@EndDateExclusive IS NULL OR s.created_at < @EndDateExclusive)
              AND (
                    @NormalizedStatus = ''
                    OR (@NormalizedStatus = 'Anulada' AND s.state::text = @CancelledState)
                    OR (@NormalizedStatus = 'Registrada' AND s.state::text <> @CancelledState)
                )
              AND (@PaymentMethod = '' OR COALESCE(s.payment_method, @DefaultPaymentMethod) = @PaymentMethod)
            ORDER BY {orderByClause}
            LIMIT @Take;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var rows = await connection.QueryAsync<SaleSummaryResponse>(
                new CommandDefinition(
                    query,
                    new
                    {
                        Take = Math.Clamp(take <= 0 ? 20 : take, 1, 500),
                        StartDate = startDate,
                        EndDateExclusive = endDateExclusive,
                        NormalizedStatus = normalizedStatus,
                        PaymentMethod = NormalizeRequiredText(paymentMethod),
                        CancelledState,
                        DefaultChannel,
                        DefaultPaymentMethod,
                        FallbackCodePrefix = BuildSaleCodePrefix(DateTime.Now.Year)
                    },
                    cancellationToken: cancellationToken));

            return Result<IReadOnlyList<SaleSummaryResponse>>.Success(rows.AsList());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudieron consultar las ventas recientes.");
            return Result<IReadOnlyList<SaleSummaryResponse>>.Internal("RecentSalesFailed", "No se pudieron consultar las ventas recientes.");
        }
    }

    public async Task<Result<SalesMetricsResponse>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        const string query = """
            SELECT
                COALESCE(SUM(CASE WHEN state::text <> @CancelledState THEN 1 ELSE 0 END), 0) AS RegisteredSales,
                COALESCE(SUM(CASE WHEN state::text = @CancelledState THEN 1 ELSE 0 END), 0) AS CancelledSales,
                COALESCE(SUM(CASE WHEN state::text <> @CancelledState THEN total_price ELSE 0 END), 0) AS RegisteredAmount,
                COALESCE(SUM(CASE WHEN state::text = @CancelledState THEN total_price ELSE 0 END), 0) AS CancelledAmount,
                COALESCE(SUM(CASE
                    WHEN state::text <> @CancelledState
                     AND created_at >= CURRENT_DATE
                     AND created_at < (CURRENT_DATE + INTERVAL '1 day')
                    THEN 1 ELSE 0 END), 0) AS SalesToday,
                COALESCE(SUM(CASE
                    WHEN state::text <> @CancelledState
                     AND created_at >= CURRENT_DATE
                     AND created_at < (CURRENT_DATE + INTERVAL '1 day')
                    THEN total_price ELSE 0 END), 0) AS SalesTodayAmount,
                COALESCE(AVG(CASE
                    WHEN state::text <> @CancelledState
                     AND created_at >= CURRENT_DATE
                     AND created_at < (CURRENT_DATE + INTERVAL '1 day')
                    THEN total_price ELSE NULL END), 0) AS AverageTicket
            FROM sales;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var metrics = await connection.QuerySingleAsync<SalesMetricsResponse>(
                new CommandDefinition(query, new { CancelledState }, cancellationToken: cancellationToken));

            return Result<SalesMetricsResponse>.Success(metrics);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudieron consultar las métricas de ventas.");
            return Result<SalesMetricsResponse>.Internal("MetricsFailed", "No se pudieron consultar las métricas de ventas.");
        }
    }

    public async Task<Result<DailyCashClosingReportResponse>> GetDailyCashClosingReportAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        if (businessDate == default)
        {
            return Result<DailyCashClosingReportResponse>.Validation("businessDate", "InvalidBusinessDate", "La fecha del cierre diario es inválida.");
        }

        var startDate = businessDate.ToDateTime(TimeOnly.MinValue);
        var endDate = businessDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        const string totalsQuery = """
            SELECT
                COALESCE(SUM(CASE WHEN state::text <> @CancelledState THEN 1 ELSE 0 END), 0) AS RegisteredSalesCount,
                COALESCE(SUM(CASE WHEN state::text = @CancelledState THEN 1 ELSE 0 END), 0) AS CancelledSalesCount,
                COALESCE(SUM(CASE WHEN state::text <> @CancelledState THEN total_price ELSE 0 END), 0) AS RegisteredAmountTotal,
                COALESCE(SUM(CASE WHEN state::text = @CancelledState THEN total_price ELSE 0 END), 0) AS CancelledAmountTotal,
                COALESCE(AVG(CASE WHEN state::text <> @CancelledState THEN total_price ELSE NULL END), 0) AS AverageTicket
            FROM sales
            WHERE created_at >= @StartDate
              AND created_at < @EndDate;
            """;

        const string paymentMethodsQuery = """
            SELECT
                COALESCE(payment_method, @DefaultPaymentMethod) AS PaymentMethod,
                COUNT(*) AS SalesCount,
                COALESCE(SUM(total_price), 0) AS TotalAmount
            FROM sales
            WHERE created_at >= @StartDate
              AND created_at < @EndDate
              AND state::text <> @CancelledState
            GROUP BY COALESCE(payment_method, @DefaultPaymentMethod)
            ORDER BY TotalAmount DESC, PaymentMethod ASC;
            """;

        const string productsQuery = """
            SELECT
                d.product_id AS ProductId,
                COALESCE(d.product_name_snapshot, CONCAT('Producto ', d.product_id::text)) AS ProductName,
                COALESCE(SUM(d.quantity), 0) AS QuantitySold,
                COALESCE(AVG(d.unit_price), 0) AS AverageUnitPrice,
                COALESCE(SUM(d.subtotal), 0) AS TotalAmount
            FROM sale_details d
            INNER JOIN sales s ON s.id = d.sale_id
            WHERE s.created_at >= @StartDate
              AND s.created_at < @EndDate
              AND s.state::text <> @CancelledState
            GROUP BY d.product_id, COALESCE(d.product_name_snapshot, CONCAT('Producto ', d.product_id::text))
            ORDER BY TotalAmount DESC, QuantitySold DESC, ProductName ASC;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var totals = await connection.QuerySingleAsync<DailyTotalsRow>(
                new CommandDefinition(
                    totalsQuery,
                    new { CancelledState, StartDate = startDate, EndDate = endDate },
                    cancellationToken: cancellationToken));

            var paymentMethods = (await connection.QueryAsync<DailyPaymentMethodSummaryResponse>(
                new CommandDefinition(
                    paymentMethodsQuery,
                    new
                    {
                        CancelledState,
                        StartDate = startDate,
                        EndDate = endDate,
                        DefaultPaymentMethod
                    },
                    cancellationToken: cancellationToken))).AsList();

            var products = (await connection.QueryAsync<DailyProductSalesSummaryResponse>(
                new CommandDefinition(
                    productsQuery,
                    new { CancelledState, StartDate = startDate, EndDate = endDate },
                    cancellationToken: cancellationToken))).AsList();

            return Result<DailyCashClosingReportResponse>.Success(new DailyCashClosingReportResponse(
                businessDate,
                DateTime.Now,
                totals.RegisteredSalesCount,
                totals.CancelledSalesCount,
                totals.RegisteredAmountTotal,
                totals.CancelledAmountTotal,
                totals.AverageTicket,
                paymentMethods,
                products));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo generar el cierre diario de caja.");
            return Result<DailyCashClosingReportResponse>.Internal("DailyCashClosingFailed", "No se pudo generar el cierre diario de caja.");
        }
    }

    public async Task<Result<SaleDetailResponse>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        if (saleId <= 0)
        {
            return Result<SaleDetailResponse>.Validation("saleId", "InvalidSaleId", "El identificador de la venta es inválido.");
        }

        const string headerQuery = """
            SELECT
                s.id::bigint AS Id,
                COALESCE(s.code, @FallbackCodePrefix || LPAD(s.id::text, 6, '0')) AS Code,
                s.created_at AS CreatedAt,
                COALESCE(
                    c.ci_nit,
                    CASE
                        WHEN c.complement IS NULL OR BTRIM(c.complement) = '' THEN c.ci::text
                        ELSE CONCAT(c.ci::text, '-', c.complement)
                    END,
                    '0') AS CustomerCiNit,
                c.razon_social AS CustomerBusinessName,
                COALESCE(s.channel, @DefaultChannel) AS Channel,
                COALESCE(s.payment_method, @DefaultPaymentMethod) AS PaymentMethod,
                CASE
                    WHEN s.state::text = @CancelledState THEN 'Anulada'
                    ELSE 'Registrada'
                END AS Status,
                s.total_price AS Total
            FROM sales s
            INNER JOIN customer c ON c.id = s.customer_id
            WHERE s.id = @SaleId;
            """;

        const string linesQuery = """
            SELECT
                product_id::bigint AS ProductId,
                COALESCE(product_name_snapshot, CONCAT('Producto ', product_id::text)) AS ProductName,
                COALESCE(lot_code, '') AS LotCode,
                quantity AS Quantity,
                unit_price AS UnitPrice,
                subtotal AS Subtotal
            FROM sale_details
            WHERE sale_id = @SaleId
            ORDER BY id ASC;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var header = await connection.QuerySingleOrDefaultAsync<SaleDetailHeaderRow>(
                new CommandDefinition(
                    headerQuery,
                    new
                    {
                        SaleId = saleId,
                        CancelledState,
                        DefaultChannel,
                        DefaultPaymentMethod,
                        FallbackCodePrefix = BuildSaleCodePrefix(DateTime.Now.Year)
                    },
                    cancellationToken: cancellationToken));

            if (header is null)
            {
                return Result<SaleDetailResponse>.NotFound("SaleNotFound", "La venta solicitada no existe.");
            }

            var lines = (await connection.QueryAsync<SaleDetailLineResponse>(
                new CommandDefinition(linesQuery, new { SaleId = saleId }, cancellationToken: cancellationToken))).AsList();

            return Result<SaleDetailResponse>.Success(new SaleDetailResponse(
                header.Id,
                header.Code,
                header.CreatedAt,
                header.CustomerCiNit,
                header.CustomerBusinessName,
                header.Channel,
                header.PaymentMethod,
                header.Status,
                header.Total,
                lines));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo consultar el detalle de la venta {SaleId}.", saleId);
            return Result<SaleDetailResponse>.Internal("SaleDetailFailed", "No se pudo consultar el detalle de la venta.");
        }
    }

    public async Task<Result<SaleReceiptResponse>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        var detailResult = await GetSaleDetailAsync(saleId, cancellationToken);
        if (detailResult.IsFailure)
        {
            return Result<SaleReceiptResponse>.Failure(detailResult.Errors);
        }

        const string actorQuery = """
            SELECT COALESCE(operator_username, CONCAT('usuario-', operator_id::text)) AS CreatedBy
            FROM sales
            WHERE id = @SaleId;
            """;

        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var createdBy = await connection.ExecuteScalarAsync<string?>(
                new CommandDefinition(actorQuery, new { SaleId = saleId }, cancellationToken: cancellationToken))
                ?? "sistema";

            var detail = detailResult.Value;
            var receiptLines = detail.Lines
                .Select(line => new SaleReceiptLineResponse(
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice,
                    line.Subtotal))
                .ToList();

            return Result<SaleReceiptResponse>.Success(new SaleReceiptResponse(
                detail.Id,
                detail.Code,
                detail.CreatedAt,
                DateTime.Now,
                detail.CustomerCiNit,
                detail.CustomerBusinessName,
                createdBy,
                detail.Total,
                FormatAmountInWords(detail.Total),
                receiptLines));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo consultar el comprobante de la venta {SaleId}.", saleId);
            return Result<SaleReceiptResponse>.Internal("SaleReceiptFailed", "No se pudo consultar el comprobante de la venta.");
        }
    }

    public async Task<Result<SaleReceiptResponse>> RegisterSaleAsync(
        RegisterSaleRequest request,
        SalesActor actor,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateRegisterRequest(request);
        if (validationResult.IsFailure)
        {
            return Result<SaleReceiptResponse>.Failure(validationResult.Errors);
        }

        var distinctProductIds = request.Lines
            .Select(line => line.ProductId)
            .Distinct()
            .ToList();

        var productsById = new Dictionary<long, CatalogApiClient.CatalogProductResponse>();
        foreach (var productId in distinctProductIds)
        {
            var productResult = await catalogApiClient.GetProductByIdAsync(productId, cancellationToken);
            if (productResult.IsFailure)
            {
                return Result<SaleReceiptResponse>.Failure(productResult.Errors);
            }

            productsById[productId] = productResult.Value;
        }

        var productQuantityMap = request.Lines
            .GroupBy(line => line.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));

        foreach (var line in request.Lines)
        {
            var product = productsById[line.ProductId];
            if (!string.IsNullOrWhiteSpace(line.LotCode)
                && !string.Equals(line.LotCode.Trim(), product.Batch, StringComparison.Ordinal))
            {
                return Result<SaleReceiptResponse>.Validation(
                    "Lines",
                    "InvalidLotCode",
                    $"El lote proporcionado para el producto {product.Name} no coincide con el catálogo.");
            }
        }

        foreach (var (productId, requestedQuantity) in productQuantityMap)
        {
            var product = productsById[productId];
            if (requestedQuantity > product.Stock)
            {
                return Result<SaleReceiptResponse>.Validation(
                    "Lines",
                    "InsufficientStock",
                    $"No hay stock suficiente para el producto {product.Name}.");
            }
        }

        var reservedLines = new List<ReservedStockLine>();
        try
        {
            foreach (var (productId, quantity) in productQuantityMap)
            {
                await WriteSagaLogAsync(
                    saleId: null,
                    step: "reserve-stock",
                    status: "requested",
                    actor: actor,
                    productId: productId,
                    quantity: quantity,
                    errorMessage: null,
                    payload: new { Operation = "register-sale", ProductId = productId, Quantity = quantity },
                    cancellationToken: cancellationToken);

                var reserveResult = await stockSagaCoordinator.ReserveStockAsync(
                    productId,
                    quantity,
                    actor.UserId,
                    actor.Username,
                    null,
                    cancellationToken);

                if (reserveResult.IsFailure)
                {
                    await WriteSagaLogAsync(
                        saleId: null,
                        step: "reserve-stock",
                        status: "failed",
                        actor: actor,
                        productId: productId,
                        quantity: quantity,
                        errorMessage: GetErrorSummary(reserveResult),
                        payload: new { Operation = "register-sale", ProductId = productId, Quantity = quantity },
                        cancellationToken: cancellationToken);
                    await RecoverReservedLinesAsync(reservedLines, actor, cancellationToken);
                    return Result<SaleReceiptResponse>.Failure(reserveResult.Errors);
                }

                reservedLines.Add(new ReservedStockLine(productId, quantity));
                await WriteSagaLogAsync(
                    saleId: null,
                    step: "reserve-stock",
                    status: "succeeded",
                    actor: actor,
                    productId: productId,
                    quantity: quantity,
                    errorMessage: null,
                    payload: new { Operation = "register-sale", ProductId = productId, Quantity = quantity },
                    cancellationToken: cancellationToken);
            }

            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            var transactionCommitted = false;

            try
            {
                var resolvedCustomerId = await ResolveCustomerIdAsync(connection, transaction, request, actor, cancellationToken);
                if (!resolvedCustomerId.HasValue)
                {
                    transaction.Rollback();
                    await RecoverReservedLinesAsync(reservedLines, actor, cancellationToken);
                    return Result<SaleReceiptResponse>.Validation("CustomerId", "InvalidCustomer", "No se pudo resolver el cliente de la venta.");
                }

                var customerId = resolvedCustomerId.Value;

                var total = decimal.Round(request.Lines.Sum(line =>
                {
                    var product = productsById[line.ProductId];
                    return product.Price * line.Quantity;
                }), 2, MidpointRounding.AwayFromZero);

                await EnsureSalesSequenceAsync(connection, transaction, cancellationToken);

                var insertSaleQuery = """
                    INSERT INTO sales (
                        customer_id,
                        operator_id,
                        operator_username,
                        total_price,
                        created_at,
                        state,
                        channel,
                        payment_method
                    )
                    VALUES (
                        @CustomerId,
                        @OperatorId,
                        @OperatorUsername,
                        @TotalPrice,
                        NOW(),
                        CAST(@State AS sale_state),
                        @Channel,
                        @PaymentMethod
                    )
                    RETURNING id;
                    """;

                var saleId = await connection.ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        insertSaleQuery,
                        new
                        {
                            CustomerId = customerId,
                            OperatorId = actor.UserId,
                            OperatorUsername = actor.Username,
                            TotalPrice = total,
                            State = RegisteredState,
                            Channel = NormalizeRequiredText(request.Channel, DefaultChannel),
                            PaymentMethod = NormalizeRequiredText(request.PaymentMethod, DefaultPaymentMethod)
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                var saleCode = FormatSaleCode(DateTime.Now.Year, saleId);
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "UPDATE sales SET code = @Code WHERE id = @SaleId;",
                        new { Code = saleCode, SaleId = saleId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                const string insertDetailQuery = """
                    INSERT INTO sale_details (
                        product_id,
                        product_name_snapshot,
                        lot_code,
                        unit_price,
                        quantity,
                        sale_id,
                        created_by_user_id,
                        updated_by_user_id
                    )
                    VALUES (
                        @ProductId,
                        @ProductNameSnapshot,
                        @LotCode,
                        @UnitPrice,
                        @Quantity,
                        @SaleId,
                        @CreatedByUserId,
                        @UpdatedByUserId
                    );
                    """;

                foreach (var line in request.Lines)
                {
                    var product = productsById[line.ProductId];
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertDetailQuery,
                            new
                            {
                                ProductId = line.ProductId,
                                ProductNameSnapshot = product.Name,
                                LotCode = string.IsNullOrWhiteSpace(line.LotCode) ? product.Batch : line.LotCode.Trim(),
                                UnitPrice = product.Price,
                                Quantity = line.Quantity,
                                SaleId = saleId,
                                CreatedByUserId = actor.UserId,
                                UpdatedByUserId = actor.UserId
                            },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                await WriteSalesAuditLogAsync(
                    connection,
                    transaction,
                    saleId,
                    action: "CREATE",
                    actor: actor,
                    previousState: null,
                    currentState: RegisteredState,
                    previousData: null,
                    newData: new
                    {
                        SaleId = saleId,
                        Code = saleCode,
                        CustomerId = customerId,
                        OperatorId = actor.UserId,
                        OperatorUsername = actor.Username,
                        Channel = NormalizeRequiredText(request.Channel, DefaultChannel),
                        PaymentMethod = NormalizeRequiredText(request.PaymentMethod, DefaultPaymentMethod),
                        Total = total,
                        Lines = request.Lines.Select(line => new
                        {
                            line.ProductId,
                            line.Quantity
                        }).ToList()
                    },
                    cancellationToken: cancellationToken);

                transaction.Commit();
                transactionCommitted = true;

                var detailResult = await GetSaleDetailAsync(saleId, cancellationToken);
                if (detailResult.IsFailure)
                {
                    return Result<SaleReceiptResponse>.Failure(detailResult.Errors);
                }

                var receipt = BuildReceiptResponse(detailResult.Value, actor.Username);

                try
                {
                    await eventPublisher.PublishAsync(
                        "sales.registered",
                        new SaleRegisteredEvent(
                            saleId,
                            saleCode,
                            detailResult.Value.CreatedAt,
                            actor.UserId,
                            actor.Username,
                            customerId,
                            detailResult.Value.CustomerCiNit,
                            detailResult.Value.CustomerBusinessName,
                            detailResult.Value.Channel,
                            detailResult.Value.PaymentMethod,
                            detailResult.Value.Total,
                            receipt.AmountInWords,
                            detailResult.Value.Lines
                                .Select(line => new SaleRegisteredLineEvent(
                                    line.ProductId,
                                    line.ProductName,
                                    line.LotCode,
                                    line.Quantity,
                                    line.UnitPrice,
                                    line.Subtotal))
                                .ToList()),
                        saleId.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception publishException)
                {
                    logger.LogError(publishException, "La venta {SaleId} se registró, pero no se pudo publicar el evento de reportes.", saleId);
                }

                return Result<SaleReceiptResponse>.Success(receipt);
            }
            catch (Exception exception)
            {
                if (!transactionCommitted)
                {
                    transaction.Rollback();
                    await RecoverReservedLinesAsync(reservedLines, actor, cancellationToken);
                }

                logger.LogError(exception, "No se pudo registrar la venta.");
                return Result<SaleReceiptResponse>.Internal("RegisterSaleFailed", "No se pudo registrar la venta.");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló la preparación del registro de venta.");
            return Result<SaleReceiptResponse>.Internal("RegisterSaleFailed", "No se pudo registrar la venta.");
        }
    }

    public async Task<Result<bool>> CancelSaleAsync(
        long saleId,
        string reason,
        SalesActor actor,
        CancellationToken cancellationToken = default)
    {
        if (saleId <= 0)
        {
            return Result<bool>.Validation("saleId", "InvalidSaleId", "El identificador de la venta es inválido.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result<bool>.Validation("reason", "RequiredReason", "El motivo de anulación es obligatorio.");
        }

        var detailResult = await GetSaleDetailAsync(saleId, cancellationToken);
        if (detailResult.IsFailure)
        {
            return Result<bool>.Failure(detailResult.Errors);
        }

        if (string.Equals(detailResult.Value.Status, "Anulada", StringComparison.Ordinal))
        {
            return Result<bool>.Validation("saleId", "AlreadyCancelled", "La venta ya se encuentra anulada.");
        }

        var recoveredLines = new List<ReservedStockLine>();
        try
        {
            foreach (var groupedLine in detailResult.Value.Lines.GroupBy(line => line.ProductId))
            {
                var quantity = groupedLine.Sum(line => line.Quantity);
                await WriteSagaLogAsync(
                    saleId,
                    step: "recover-stock",
                    status: "requested",
                    actor: actor,
                    productId: groupedLine.Key,
                    quantity: quantity,
                    errorMessage: null,
                    payload: new { Operation = "cancel-sale", SaleId = saleId, ProductId = groupedLine.Key, Quantity = quantity },
                    cancellationToken: cancellationToken);

                var recoverResult = await stockSagaCoordinator.RecoverStockAsync(
                    groupedLine.Key,
                    quantity,
                    actor.UserId,
                    actor.Username,
                    saleId,
                    cancellationToken);

                if (recoverResult.IsFailure)
                {
                    await WriteSagaLogAsync(
                        saleId,
                        step: "recover-stock",
                        status: "failed",
                        actor: actor,
                        productId: groupedLine.Key,
                        quantity: quantity,
                        errorMessage: GetErrorSummary(recoverResult),
                        payload: new { Operation = "cancel-sale", SaleId = saleId, ProductId = groupedLine.Key, Quantity = quantity },
                        cancellationToken: cancellationToken);
                    await ReReserveRecoveredLinesAsync(recoveredLines, actor, cancellationToken);
                    return Result<bool>.Failure(recoverResult.Errors);
                }

                recoveredLines.Add(new ReservedStockLine(groupedLine.Key, quantity));
                await WriteSagaLogAsync(
                    saleId,
                    step: "recover-stock",
                    status: "succeeded",
                    actor: actor,
                    productId: groupedLine.Key,
                    quantity: quantity,
                    errorMessage: null,
                    payload: new { Operation = "cancel-sale", SaleId = saleId, ProductId = groupedLine.Key, Quantity = quantity },
                    cancellationToken: cancellationToken);
            }

            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            var transactionCommitted = false;
            var normalizedReason = NormalizeRequiredText(reason);
            var cancelledAt = DateTime.Now;

            try
            {
                var affectedRows = await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE sales
                        SET state = CAST(@CancelledState AS sale_state),
                            cancelled_by_user_id = @CancelledByUserId,
                            cancelled_by_username = @CancelledByUsername,
                            cancelled_at = @CancelledAt
                        WHERE id = @SaleId
                          AND state::text <> @CancelledState;
                        """,
                        new
                        {
                            CancelledState,
                            CancelledByUserId = actor.UserId,
                            CancelledByUsername = actor.Username,
                            CancelledAt = cancelledAt,
                            SaleId = saleId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    await ReReserveRecoveredLinesAsync(recoveredLines, actor, cancellationToken);
                    return Result<bool>.Validation("saleId", "AlreadyCancelled", "La venta no pudo anularse o ya se encontraba anulada.");
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM cancel_reason WHERE sale_id = @SaleId;",
                        new { SaleId = saleId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO cancel_reason (
                            sale_id,
                            cancel_reason,
                            operator_id,
                            operator_username,
                            created_by_user_id,
                            updated_by_user_id,
                            created_at
                        )
                        VALUES (
                            @SaleId,
                            @Reason,
                            @OperatorId,
                            @OperatorUsername,
                            @CreatedByUserId,
                            @UpdatedByUserId,
                            @CreatedAt
                        );
                        """,
                        new
                        {
                            SaleId = saleId,
                            Reason = normalizedReason,
                            OperatorId = actor.UserId,
                            OperatorUsername = actor.Username,
                            CreatedByUserId = actor.UserId,
                            UpdatedByUserId = actor.UserId,
                            CreatedAt = cancelledAt
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await WriteSalesAuditLogAsync(
                    connection,
                    transaction,
                    saleId,
                    action: "UPDATE",
                    actor: actor,
                    previousState: RegisteredState,
                    currentState: CancelledState,
                    previousData: new
                    {
                        SaleId = saleId,
                        State = RegisteredState
                    },
                    newData: new
                    {
                        SaleId = saleId,
                        State = CancelledState,
                        Reason = normalizedReason,
                        CancelledByUserId = actor.UserId,
                        CancelledByUsername = actor.Username,
                        CancelledAt = cancelledAt
                    },
                    cancellationToken: cancellationToken);

                transaction.Commit();
                transactionCommitted = true;

                try
                {
                    await eventPublisher.PublishAsync(
                        "sales.cancelled",
                        new SaleCancelledEvent(
                            saleId,
                            normalizedReason,
                            actor.UserId,
                            actor.Username,
                            cancelledAt),
                        saleId.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception publishException)
                {
                    logger.LogError(publishException, "La venta {SaleId} se anuló, pero no se pudo publicar el evento de reportes.", saleId);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                if (!transactionCommitted)
                {
                    transaction.Rollback();
                    await ReReserveRecoveredLinesAsync(recoveredLines, actor, cancellationToken);
                }

                logger.LogError(exception, "No se pudo anular la venta {SaleId}.", saleId);
                return Result<bool>.Internal("CancelSaleFailed", "No se pudo anular la venta.");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló la preparación de la anulación de la venta {SaleId}.", saleId);
            return Result<bool>.Internal("CancelSaleFailed", "No se pudo anular la venta.");
        }
    }

    private async Task<Result<string>> GetNextSaleCodeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            var nextId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    "SELECT COALESCE(MAX(id), 0) + 1 FROM sales;",
                    cancellationToken: cancellationToken));

            return Result<string>.Success(FormatSaleCode(DateTime.Now.Year, nextId));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo calcular el siguiente código de venta.");
            return Result<string>.Internal("NextSaleCodeFailed", "No se pudo calcular el siguiente código de venta.");
        }
    }

    private async Task<long?> ResolveCustomerIdAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        RegisterSaleRequest request,
        SalesActor actor,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId.HasValue)
        {
            var customerId = request.CustomerId.Value;
            if (customerId < 0)
            {
                return null;
            }

            var exists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT EXISTS(SELECT 1 FROM customer WHERE id = @CustomerId);",
                    new { CustomerId = customerId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            return exists ? customerId : null;
        }

        if (request.NewCustomer is null)
        {
            return null;
        }

        var normalizedCiNit = NormalizeRequiredText(request.NewCustomer.CiNit);
        var parsedCi = ParseCiNumber(normalizedCiNit);

        const string insertCustomerQuery = """
            INSERT INTO customer (
                ci,
                complement,
                ci_nit,
                razon_social,
                phone,
                email,
                address,
                created_by,
                created_at
            )
            VALUES (
                @Ci,
                NULL,
                @CiNit,
                @RazonSocial,
                @Phone,
                @Email,
                @Address,
                @CreatedBy,
                NOW()
            )
            RETURNING id;
            """;

        await EnsureCustomerSequenceAsync(connection, transaction, cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                insertCustomerQuery,
                new
                {
                    Ci = parsedCi,
                    CiNit = normalizedCiNit,
                    RazonSocial = NormalizeRequiredText(request.NewCustomer.BusinessName),
                    Phone = NormalizeOptionalText(request.NewCustomer.Phone),
                    Email = NormalizeOptionalText(request.NewCustomer.Email),
                    Address = NormalizeOptionalText(request.NewCustomer.Address),
                    CreatedBy = actor.UserId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }

    private static Result ValidateRegisterRequest(RegisterSaleRequest? request)
    {
        if (request is null)
        {
            return Result.Validation("request", "MissingRequest", "La venta es obligatoria.");
        }

        var errors = new List<AppError>();
        if (string.IsNullOrWhiteSpace(request.Channel))
        {
            errors.Add(new AppError("RequiredChannel", "El canal es obligatorio.", ErrorType.Validation, "Channel"));
        }
        else if (NormalizeRequiredText(request.Channel).Length > 30)
        {
            errors.Add(new AppError("InvalidChannel", "El canal no puede exceder 30 caracteres.", ErrorType.Validation, "Channel"));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            errors.Add(new AppError("RequiredPaymentMethod", "El método de pago es obligatorio.", ErrorType.Validation, "PaymentMethod"));
        }
        else if (NormalizeRequiredText(request.PaymentMethod).Length > 30)
        {
            errors.Add(new AppError("InvalidPaymentMethod", "El método de pago no puede exceder 30 caracteres.", ErrorType.Validation, "PaymentMethod"));
        }

        if (!request.CustomerId.HasValue && request.NewCustomer is null)
        {
            errors.Add(new AppError("MissingCustomer", "Debes seleccionar un cliente o registrar uno nuevo.", ErrorType.Validation, "CustomerId"));
        }
        else if (request.CustomerId.HasValue && request.CustomerId.Value < 0)
        {
            errors.Add(new AppError("InvalidCustomer", "El cliente seleccionado no es válido.", ErrorType.Validation, "CustomerId"));
        }

        if (request.NewCustomer is not null)
        {
            if (string.IsNullOrWhiteSpace(request.NewCustomer.CiNit))
            {
                errors.Add(new AppError("RequiredCiNit", "El CI/NIT es obligatorio.", ErrorType.Validation, "NewCustomer.CiNit"));
            }

            if (string.IsNullOrWhiteSpace(request.NewCustomer.BusinessName))
            {
                errors.Add(new AppError("RequiredBusinessName", "La razón social es obligatoria.", ErrorType.Validation, "NewCustomer.BusinessName"));
            }
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            errors.Add(new AppError("MissingLines", "Debes agregar al menos un producto a la venta.", ErrorType.Validation, "Lines"));
        }
        else
        {
            foreach (var line in request.Lines)
            {
                if (line.ProductId <= 0)
                {
                    errors.Add(new AppError("InvalidProduct", "Hay un producto inválido en el detalle de la venta.", ErrorType.Validation, "Lines"));
                }

                if (line.Quantity <= 0)
                {
                    errors.Add(new AppError("InvalidQuantity", "La cantidad de cada producto debe ser mayor a cero.", ErrorType.Validation, "Lines"));
                }
            }
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    private async Task RecoverReservedLinesAsync(
        IReadOnlyCollection<ReservedStockLine> reservedLines,
        SalesActor actor,
        CancellationToken cancellationToken)
    {
        foreach (var line in reservedLines)
        {
            await WriteSagaLogAsync(
                saleId: null,
                step: "recover-stock-compensation",
                status: "requested",
                actor: actor,
                productId: line.ProductId,
                quantity: line.Quantity,
                errorMessage: null,
                payload: new { Operation = "register-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                cancellationToken: cancellationToken);

            var recoverResult = await stockSagaCoordinator.RecoverStockAsync(
                line.ProductId,
                line.Quantity,
                actor.UserId,
                actor.Username,
                null,
                cancellationToken);

            if (recoverResult.IsFailure)
            {
                await WriteSagaLogAsync(
                    saleId: null,
                    step: "recover-stock-compensation",
                    status: "failed",
                    actor: actor,
                    productId: line.ProductId,
                    quantity: line.Quantity,
                    errorMessage: GetErrorSummary(recoverResult),
                    payload: new { Operation = "register-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                    cancellationToken: cancellationToken);
                logger.LogWarning(
                    "No se pudo compensar la reserva de stock del producto {ProductId}.",
                    line.ProductId);
                continue;
            }

            await WriteSagaLogAsync(
                saleId: null,
                step: "recover-stock-compensation",
                status: "succeeded",
                actor: actor,
                productId: line.ProductId,
                quantity: line.Quantity,
                errorMessage: null,
                payload: new { Operation = "register-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                cancellationToken: cancellationToken);
        }
    }

    private async Task ReReserveRecoveredLinesAsync(
        IReadOnlyCollection<ReservedStockLine> recoveredLines,
        SalesActor actor,
        CancellationToken cancellationToken)
    {
        foreach (var line in recoveredLines)
        {
            await WriteSagaLogAsync(
                saleId: null,
                step: "reserve-stock-compensation",
                status: "requested",
                actor: actor,
                productId: line.ProductId,
                quantity: line.Quantity,
                errorMessage: null,
                payload: new { Operation = "cancel-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                cancellationToken: cancellationToken);

            var reserveResult = await stockSagaCoordinator.ReserveStockAsync(
                line.ProductId,
                line.Quantity,
                actor.UserId,
                actor.Username,
                null,
                cancellationToken);

            if (reserveResult.IsFailure)
            {
                await WriteSagaLogAsync(
                    saleId: null,
                    step: "reserve-stock-compensation",
                    status: "failed",
                    actor: actor,
                    productId: line.ProductId,
                    quantity: line.Quantity,
                    errorMessage: GetErrorSummary(reserveResult),
                    payload: new { Operation = "cancel-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                    cancellationToken: cancellationToken);
                logger.LogWarning(
                    "No se pudo compensar la recuperación de stock del producto {ProductId}.",
                    line.ProductId);
                continue;
            }

            await WriteSagaLogAsync(
                saleId: null,
                step: "reserve-stock-compensation",
                status: "succeeded",
                actor: actor,
                productId: line.ProductId,
                quantity: line.Quantity,
                errorMessage: null,
                payload: new { Operation = "cancel-sale-compensation", ProductId = line.ProductId, Quantity = line.Quantity },
                cancellationToken: cancellationToken);
        }
    }

    private async Task WriteSalesAuditLogAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long saleId,
        string action,
        SalesActor actor,
        string? previousState,
        string? currentState,
        object? previousData,
        object? newData,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO sales_audit_log (
                        sale_id,
                        action,
                        actor_user_id,
                        actor_username,
                        previous_state,
                        current_state,
                        previous_data,
                        new_data,
                        created_at
                    )
                    VALUES (
                        @SaleId,
                        @Action,
                        @ActorUserId,
                        @ActorUsername,
                        CAST(@PreviousState AS sale_state),
                        CAST(@CurrentState AS sale_state),
                        CAST(@PreviousData AS jsonb),
                        CAST(@NewData AS jsonb),
                        NOW()
                    );
                    """,
                    new
                    {
                        SaleId = saleId,
                        Action = action,
                        ActorUserId = actor.UserId,
                        ActorUsername = actor.Username,
                        PreviousState = previousState,
                        CurrentState = currentState,
                        PreviousData = SerializeJson(previousData),
                        NewData = SerializeJson(newData)
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "No se pudo registrar la auditoría de ventas para la venta {SaleId}.", saleId);
        }
    }

    private async Task WriteSagaLogAsync(
        long? saleId,
        string step,
        string status,
        SalesActor actor,
        long? productId,
        int? quantity,
        string? errorMessage,
        object? payload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO sales_saga_log (
                        sale_id,
                        step,
                        status,
                        actor_user_id,
                        actor_username,
                        product_id,
                        quantity,
                        error_message,
                        payload,
                        created_at
                    )
                    VALUES (
                        @SaleId,
                        @Step,
                        @Status,
                        @ActorUserId,
                        @ActorUsername,
                        @ProductId,
                        @Quantity,
                        @ErrorMessage,
                        CAST(@Payload AS jsonb),
                        NOW()
                    );
                    """,
                    new
                    {
                        SaleId = saleId,
                        Step = step,
                        Status = status,
                        ActorUserId = actor.UserId,
                        ActorUsername = actor.Username,
                        ProductId = productId,
                        Quantity = quantity,
                        ErrorMessage = errorMessage,
                        Payload = SerializeJson(payload)
                    },
                    cancellationToken: cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "No se pudo registrar la bitácora de saga {Step}/{Status} para la venta {SaleId}.",
                step,
                status,
                saleId);
        }
    }

    private static string GetErrorSummary(Result result)
    {
        var messages = result.Errors
            .Select(error => error.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return messages.Count > 0
            ? string.Join("; ", messages)
            : "La operación no pudo completarse.";
    }

    private static string? SerializeJson(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value);

    private static string BuildRecentSalesOrderBy(string? sortBy, string? sortDirection)
    {
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy)
            ? "createdat"
            : sortBy.Trim().ToLowerInvariant();
        var direction = string.Equals(sortDirection?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";
        var reverseDirection = direction == "ASC" ? "DESC" : "ASC";

        return normalizedSortBy switch
        {
            "code" => $"COALESCE(s.code, '') {direction}, s.id {direction}",
            "customer" => $"c.razon_social {direction}, s.id {direction}",
            "channel" => $"COALESCE(s.channel, '{DefaultChannel}') {direction}, s.id {direction}",
            "paymentmethod" => $"COALESCE(s.payment_method, '{DefaultPaymentMethod}') {direction}, s.id {direction}",
            "total" => $"s.total_price {direction}, s.id {direction}",
            "status" => $"CASE WHEN s.state::text = '{CancelledState}' THEN 1 ELSE 0 END {direction}, s.id {reverseDirection}",
            _ => $"s.created_at {direction}, s.id {direction}"
        };
    }

    private static string NormalizeStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "anulada" => "Anulada",
            "registrada" => "Registrada",
            _ => string.Empty
        };
    }

    private static string BuildLikeSearchValue(string? searchTerm) =>
        $"%{(searchTerm ?? string.Empty).Trim()}%";

    private static string NormalizeRequiredText(string? value, string? fallback = null)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return fallback ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = NormalizeRequiredText(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int ParseCiNumber(string ciNit)
    {
        var numericPart = new string(ciNit.Where(char.IsDigit).ToArray());
        return int.TryParse(numericPart, out var value) ? value : 0;
    }

    private static string FormatSaleCode(int year, long saleId) =>
        $"{BuildSaleCodePrefix(year)}{saleId.ToString("D6", CultureInfo.InvariantCulture)}";

    private static string BuildSaleCodePrefix(int year) =>
        $"VTA-{year}-";

    private static Task EnsureSalesSequenceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken) =>
        EnsureIdentitySequenceAsync(connection, transaction, "sales", cancellationToken);

    private static Task EnsureCustomerSequenceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken) =>
        EnsureIdentitySequenceAsync(connection, transaction, "customer", cancellationToken);

    private static async Task EnsureIdentitySequenceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        var commandText = $"""
            SELECT setval(
                pg_get_serial_sequence('{tableName}', 'id'),
                CASE
                    WHEN COALESCE((SELECT MAX(id) FROM {tableName}), 0) > 0
                        THEN (SELECT MAX(id) FROM {tableName})
                    ELSE 1
                END,
                COALESCE((SELECT MAX(id) FROM {tableName}), 0) > 0
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            commandText,
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static string FormatAmountInWords(decimal total) =>
        $"SON {total.ToString("N2", CultureInfo.InvariantCulture)} BOLIVIANOS";

    private static SaleReceiptResponse BuildReceiptResponse(SaleDetailResponse detail, string createdBy)
    {
        var receiptLines = detail.Lines
            .Select(line => new SaleReceiptLineResponse(
                line.ProductName,
                line.Quantity,
                line.UnitPrice,
                line.Subtotal))
            .ToList();

        return new SaleReceiptResponse(
            detail.Id,
            detail.Code,
            detail.CreatedAt,
            DateTime.Now,
            detail.CustomerCiNit,
            detail.CustomerBusinessName,
            createdBy,
            detail.Total,
            FormatAmountInWords(detail.Total),
            receiptLines);
    }

    private sealed record ReservedStockLine(long ProductId, int Quantity);

    public sealed record SalesActor(long UserId, string Username);

    private sealed record DailyTotalsRow(
        int RegisteredSalesCount,
        int CancelledSalesCount,
        decimal RegisteredAmountTotal,
        decimal CancelledAmountTotal,
        decimal AverageTicket);

    private sealed record SaleDetailHeaderRow(
        long Id,
        string Code,
        DateTime CreatedAt,
        string CustomerCiNit,
        string CustomerBusinessName,
        string Channel,
        string PaymentMethod,
        string Status,
        decimal Total);
}
