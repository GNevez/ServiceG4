-- Script para criar a tabela de Rótulos
-- Execute este script no banco de dados g4motocenter

CREATE TABLE IF NOT EXISTS `Rotulos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `IdPedido` INT NULL,
    `IdRecibo` VARCHAR(100) NOT NULL,
    `IdAtendimento` VARCHAR(100) NULL,
    `NomeArquivo` VARCHAR(255) NOT NULL,
    `CaminhoArquivo` VARCHAR(500) NOT NULL,
    `DataGeracao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `QuantidadeRotulos` INT NOT NULL DEFAULT 1,
    `CodigosObjeto` VARCHAR(1000) NULL,
    `IdsPrePostagem` VARCHAR(1000) NULL,
    `TipoRotulo` VARCHAR(10) NOT NULL DEFAULT 'P',
    `FormatoRotulo` VARCHAR(10) NOT NULL DEFAULT 'ET',
    `TamanhoBytes` BIGINT NOT NULL DEFAULT 0,
    `Observacao` VARCHAR(500) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Rotulos_IdPedido` (`IdPedido`),
    INDEX `IX_Rotulos_IdRecibo` (`IdRecibo`),
    INDEX `IX_Rotulos_DataGeracao` (`DataGeracao`),
    CONSTRAINT `FK_Rotulos_Pedidos` FOREIGN KEY (`IdPedido`) REFERENCES `Pedidos` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
