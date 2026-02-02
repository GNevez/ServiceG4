-- Script para criar as tabelas de Devolução
-- Executar no banco de dados g4motocenter_cadastros

-- Drop tabelas existentes se necessário (CUIDADO: apenas em desenvolvimento!)
-- DROP TABLE IF EXISTS `devolucao_itens`;
-- DROP TABLE IF EXISTS `devolucoes`;

-- Tabela principal de devoluções
CREATE TABLE IF NOT EXISTS `devolucoes` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `pedido_id` INT NOT NULL,
    `cpf` VARCHAR(20) NOT NULL,
    `nome_cliente` VARCHAR(200) NOT NULL,
    `email` VARCHAR(255) NOT NULL,
    `telefone` VARCHAR(30) NULL,
    `motivo` TEXT NULL,
    `observacoes` TEXT NULL,
    `status` INT NOT NULL DEFAULT 0,
    `data_criacao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `data_atualizacao` DATETIME NULL,
    
    -- Dados do Correios - Logística Reversa
    `id_pre_postagem` VARCHAR(100) NULL,
    `codigo_postagem` VARCHAR(100) NULL,
    `codigo_rastreamento` VARCHAR(50) NULL,
    `data_limite_postagem` DATETIME NULL,
    `url_etiqueta` VARCHAR(500) NULL,
    `resposta_correios_json` LONGTEXT NULL,
    
    PRIMARY KEY (`id`),
    INDEX `IX_devolucoes_pedido_id` (`pedido_id`),
    INDEX `IX_devolucoes_status` (`status`),
    INDEX `IX_devolucoes_data_criacao` (`data_criacao`),
    CONSTRAINT `FK_devolucoes_pedidos` FOREIGN KEY (`pedido_id`) 
        REFERENCES `pedidos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de itens da devolução
CREATE TABLE IF NOT EXISTS `devolucao_itens` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `devolucao_id` INT NOT NULL,
    `item_carrinho_id` INT NOT NULL,
    `produto_id` INT NOT NULL,
    `produto_nome` VARCHAR(500) NOT NULL,
    `grade_id` INT NULL,
    `grade_descricao` VARCHAR(200) NULL,
    `quantidade` INT NOT NULL DEFAULT 1,
    `preco_unitario` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    
    PRIMARY KEY (`id`),
    INDEX `IX_devolucao_itens_devolucao_id` (`devolucao_id`),
    INDEX `IX_devolucao_itens_item_carrinho_id` (`item_carrinho_id`),
    CONSTRAINT `FK_devolucao_itens_devolucoes` FOREIGN KEY (`devolucao_id`) 
        REFERENCES `devolucoes` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_devolucao_itens_carrinho_item` FOREIGN KEY (`item_carrinho_id`) 
        REFERENCES `carrinho_item` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Adicionar comentários às tabelas
ALTER TABLE `devolucoes` COMMENT = 'Tabela de solicitações de devolução de pedidos';
ALTER TABLE `devolucao_itens` COMMENT = 'Itens incluídos na solicitação de devolução';

-- Se a tabela já existir mas faltar colunas, usar ALTER TABLE:
-- ALTER TABLE `devolucoes` ADD COLUMN `codigo_postagem` VARCHAR(100) NULL AFTER `id_pre_postagem`;
-- ALTER TABLE `devolucoes` ADD COLUMN `data_limite_postagem` DATETIME NULL AFTER `codigo_rastreamento`;
-- ALTER TABLE `devolucoes` ADD COLUMN `url_etiqueta` VARCHAR(500) NULL AFTER `data_limite_postagem`;
-- ALTER TABLE `devolucoes` ADD COLUMN `resposta_correios_json` LONGTEXT NULL AFTER `url_etiqueta`;
