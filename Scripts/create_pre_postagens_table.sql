-- Script para criar a tabela de pré-postagens
-- Execute este script no banco de dados g4motocenter_cadastros

CREATE TABLE IF NOT EXISTS `pre_postagens` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `pedido_id` INT NOT NULL,
    `codigo_rastreamento` VARCHAR(50) NULL,
    `id_pre_postagem` VARCHAR(100) NULL,
    `numero_etiqueta` VARCHAR(50) NULL,
    `codigo_servico` VARCHAR(10) NOT NULL DEFAULT '03220',
    `nome_servico` VARCHAR(50) NOT NULL DEFAULT 'SEDEX',
    `peso` DECIMAL(10,3) NOT NULL DEFAULT 0.300,
    `altura` INT NOT NULL DEFAULT 5,
    `largura` INT NOT NULL DEFAULT 15,
    `comprimento` INT NOT NULL DEFAULT 20,
    `valor_declarado` DECIMAL(10,2) NULL,
    `status` INT NOT NULL DEFAULT 0,
    `data_criacao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `data_postagem` DATETIME NULL,
    `data_entrega` DATETIME NULL,
    `observacoes` VARCHAR(500) NULL,
    `mensagem_erro` VARCHAR(1000) NULL,
    `resposta_correios_json` LONGTEXT NULL,
    PRIMARY KEY (`id`),
    INDEX `ix_pre_postagens_pedido_id` (`pedido_id`),
    INDEX `ix_pre_postagens_status` (`status`),
    INDEX `ix_pre_postagens_codigo_rastreamento` (`codigo_rastreamento`),
    CONSTRAINT `fk_pre_postagens_pedido` FOREIGN KEY (`pedido_id`) REFERENCES `pedidos` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Verificar se a tabela foi criada
SELECT 'Tabela pre_postagens criada com sucesso!' AS resultado;
