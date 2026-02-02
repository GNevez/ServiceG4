-- Script para criar a tabela fila_impressao no banco de dados G4

CREATE TABLE IF NOT EXISTS `fila_impressao` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RotuloId` int DEFAULT NULL,
  `PedidoId` int DEFAULT NULL,
  `CodigoPedido` varchar(50) DEFAULT NULL,
  `NomeArquivo` varchar(255) NOT NULL,
  `CaminhoArquivo` varchar(500) NOT NULL,
  `Status` int NOT NULL DEFAULT 0,
  `Tentativas` int NOT NULL DEFAULT 0,
  `MaxTentativas` int NOT NULL DEFAULT 3,
  `MensagemErro` varchar(1000) DEFAULT NULL,
  `ImpressoraDestino` varchar(100) DEFAULT NULL,
  `Copias` int NOT NULL DEFAULT 1,
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DataProcessamento` datetime DEFAULT NULL,
  `DataImpressao` datetime DEFAULT NULL,
  `ClienteId` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_fila_impressao_RotuloId` (`RotuloId`),
  KEY `IX_fila_impressao_Status` (`Status`),
  KEY `IX_fila_impressao_DataCriacao` (`DataCriacao`),
  CONSTRAINT `FK_fila_impressao_Rotulos_RotuloId` FOREIGN KEY (`RotuloId`) REFERENCES `rotulos` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Tabela de rotulos (caso não exista)
CREATE TABLE IF NOT EXISTS `rotulos` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `IdPedido` int DEFAULT NULL,
  `IdRecibo` varchar(100) NOT NULL,
  `IdAtendimento` varchar(100) DEFAULT NULL,
  `NomeArquivo` varchar(255) NOT NULL,
  `CaminhoArquivo` varchar(500) NOT NULL,
  `DataGeracao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `QuantidadeRotulos` int NOT NULL DEFAULT 1,
  `CodigosObjeto` varchar(1000) DEFAULT NULL,
  `IdsPrePostagem` varchar(1000) DEFAULT NULL,
  `TipoRotulo` varchar(10) DEFAULT 'P',
  `FormatoRotulo` varchar(10) DEFAULT 'ET',
  `TamanhoBytes` bigint DEFAULT 0,
  `Observacao` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_rotulos_IdPedido` (`IdPedido`),
  KEY `IX_rotulos_IdRecibo` (`IdRecibo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
