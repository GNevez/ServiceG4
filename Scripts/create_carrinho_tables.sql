-- Script para criar as tabelas do carrinho no banco g4motocenter_cadastros

-- Tabela do carrinho
CREATE TABLE IF NOT EXISTS carrinho (
    id INT AUTO_INCREMENT PRIMARY KEY,
    token VARCHAR(100) NOT NULL UNIQUE,
    status INT NOT NULL DEFAULT 0,
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    cliente_id INT NULL,
    cupom_codigo VARCHAR(50) NULL,
    cupom_desconto DECIMAL(10,2) NULL DEFAULT 0,
    INDEX idx_token (token),
    INDEX idx_status (status),
    INDEX idx_ativo (ativo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Tabela dos itens do carrinho
CREATE TABLE IF NOT EXISTS carrinho_item (
    id INT AUTO_INCREMENT PRIMARY KEY,
    carrinho_id INT NOT NULL,
    produto_id INT NOT NULL,
    grade_id INT NULL,
    quantidade INT NOT NULL DEFAULT 1,
    preco_unitario DECIMAL(10,2) NOT NULL,
    data_adicao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    INDEX idx_carrinho_id (carrinho_id),
    INDEX idx_produto_id (produto_id),
    INDEX idx_grade_id (grade_id),
    CONSTRAINT fk_carrinho_item_carrinho FOREIGN KEY (carrinho_id) 
        REFERENCES carrinho(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
