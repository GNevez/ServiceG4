-- Script para criar as tabelas de pedidos e endereços
-- Execute este script manualmente no banco de dados

-- Tabela de Endereços de Entrega
CREATE TABLE IF NOT EXISTS `enderecos_entrega` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Cep` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Logradouro` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Numero` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Complemento` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `Bairro` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Cidade` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Uf` varchar(2) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `NomeDestinatario` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `TelefoneDestinatario` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `DataCriacao` datetime(6) NOT NULL,
    CONSTRAINT `PK_enderecos_entrega` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Tabela de Pedidos
CREATE TABLE IF NOT EXISTS `pedidos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CodigoPedido` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `CarrinhoId` int NOT NULL,
    `EnderecoEntregaId` int NOT NULL,
    `Status` int NOT NULL DEFAULT 0,
    `PrecoFrete` decimal(10,2) NULL,
    `TotalPedido` decimal(10,2) NOT NULL,
    `DescontoCupom` decimal(10,2) NOT NULL DEFAULT 0,
    `DataPedido` datetime(6) NOT NULL,
    `DataAtualizacao` datetime(6) NULL,
    `CodigoRastreamento` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `MetodoPagamento` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Observacoes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `MotivoCancelamento` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `MercadoPagoPaymentId` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `NomeCliente` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `EmailCliente` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `TelefoneCliente` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `CpfCliente` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    CONSTRAINT `PK_pedidos` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_pedidos_carrinho_CarrinhoId` FOREIGN KEY (`CarrinhoId`) REFERENCES `carrinho` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_pedidos_enderecos_entrega_EnderecoEntregaId` FOREIGN KEY (`EnderecoEntregaId`) REFERENCES `enderecos_entrega` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Índices
CREATE UNIQUE INDEX `IX_pedidos_CodigoPedido` ON `pedidos` (`CodigoPedido`);
CREATE INDEX `IX_pedidos_CarrinhoId` ON `pedidos` (`CarrinhoId`);
CREATE INDEX `IX_pedidos_EnderecoEntregaId` ON `pedidos` (`EnderecoEntregaId`);
