-- Script para criar a tabela produto_grade_imagem
-- Execute este script no MySQL/MariaDB para criar a tabela de imagens de grades

CREATE TABLE IF NOT EXISTS `produto_grade_imagem` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `id_produto_grade` INT(11) NOT NULL,
  `ordem` INT(11) NOT NULL DEFAULT 1,
  `caminho` VARCHAR(255) NOT NULL,
  `criado_em` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  INDEX `idx_produto_grade_imagem_grade` (`id_produto_grade`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Nota: FK removida para evitar problemas de compatibilidade de tipos.
-- A integridade referencial é controlada pela aplicação (cascade delete no EF Core).
