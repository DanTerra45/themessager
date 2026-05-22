CREATE TABLE `email_outbox` (
  `id` INT PRIMARY KEY NOT NULL AUTO_INCREMENT,
  `to_address` VARCHAR(100),
  `to_name` VARCHAR(50),
  `subject` VARCHAR(255),
  `plain_text_body` TEXT,
  `html_body` MEDIUMTEXT,
  `status` VARCHAR(50) NOT NULL,
  `attempts` INT,
  `next_attempt` timestamp,
  `last_attempt` timestamp,
  `sent__at_utc` timestamp,
  `last_error` timestamp,
  `created_at` timestamp,
  `updated_at` timestamp
);

CREATE TABLE `users` (
  `id` SMALLINT PRIMARY KEY NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(40) NOT NULL,
  `email` VARCHAR(100) UNIQUE NOT NULL,
  `password` VARCHAR(255) NOT NULL,
  `role` ENUM ('Admin', 'Auditor', 'Operator') NOT NULL,
  `creator_id` SMALLINT,
  `last_login` timestamp,
  `state` ENUM ('Active', 'Inactive') NOT NULL DEFAULT 'Active',
  `need_change_password` TINYINT(1) NOT NULL,
  `created_at` timestamp NOT NULL,
  `updated_at` timestamp,
  CHECK (state = 'Active' OR state ='Inactive'),
  CHECK (role = 'Admin' OR role = 'Operator' OR role = "Auditor")
);

CREATE TABLE `user_story` (
  `id` INT PRIMARY KEY NOT NULL AUTO_INCREMENT,
  `user_id` SMALLINT NOT NULL,
  `operator_id` SMALLINT NOT NULL,
  `previous_state` VARCHAR(50) NOT NULL,
  `actual_state` VARCHAR(50) NOT NULL,
  `disable_reason` TEXT,
  `created_at` timestamp,
  CHECK (previous_state = 'Active' OR previous_state ='Inactive'),
  CHECK (actual_state = 'Active' OR actual_state ='Inactive')
);

CREATE TABLE `password_reset_token` (
  `id` INT PRIMARY KEY NOT NULL,
  `user_id` SMALLINT,
  `token_hash` VARCHAR(64),
  `expires_at` timestamp,
  `used_at` timestamp,
  `created_at` timestamp
);

CREATE INDEX `idx_users_email_role` ON `users` (`email`, `role`);

CREATE INDEX `idx_users_username_state` ON `users` (`username`, `state`);

CREATE INDEX `idx_user_story_operator_id_actual_state` ON `user_story` (`operator_id`, `actual_state`);

ALTER TABLE `password_reset_token` ADD CONSTRAINT `fk_password_reset_token_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `user_story` ADD CONSTRAINT `fk_user_story_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);

ALTER TABLE `users` ADD CONSTRAINT `fk_users_creator_id` FOREIGN KEY (`creator_id`) REFERENCES `users` (`id`);
