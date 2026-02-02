-- phpMyAdmin SQL Dump
-- version 5.2.2
-- https://www.phpmyadmin.net/
--
-- Host: localhost:3306
-- Tempo de geração: 20/01/2026 às 19:20
-- Versão do servidor: 5.7.44-log-cll-lve
-- Versão do PHP: 8.4.16

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Banco de dados: `g4motocenter_cadastros`
--

-- --------------------------------------------------------

--
-- Estrutura para tabela `cadastros`
--

CREATE TABLE `cadastros` (
  `id` int(11) NOT NULL,
  `nome` varchar(20) NOT NULL,
  `sobrenome` varchar(20) NOT NULL,
  `email` varchar(120) NOT NULL,
  `senha` varchar(100) NOT NULL,
  `cpf` varchar(14) NOT NULL,
  `numero` varchar(20) NOT NULL,
  `nascimento` date DEFAULT NULL,
  `isBanned` int(1) NOT NULL DEFAULT '0',
  `cargo` varchar(20) NOT NULL DEFAULT 'Cliente',
  `Aut` varchar(40) DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Despejando dados para a tabela `cadastros`
--

INSERT INTO `cadastros` (`id`, `nome`, `sobrenome`, `email`, `senha`, `cpf`, `numero`, `nascimento`, `isBanned`, `cargo`, `Aut`) VALUES
(37, 'Guilherme', 'Neves', 'guilhermemferraz@hotmail.com', '$2y$10$YBx1NJtErdAYGYZrZIrZ9.NOx4w11OYdm4TZA.LPVibqzUxakFM.q', '054.942.671-05', '(61) 99636-0029', '2004-08-10', 0, 'Admin', '704642');

-- --------------------------------------------------------

--
-- Estrutura para tabela `carrinho`
--

CREATE TABLE `carrinho` (
  `id` int(11) NOT NULL,
  `carrinho` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `endereco` longtext COLLATE utf8mb4_unicode_ci,
  `Pagamento` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Pendente'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Despejando dados para a tabela `carrinho`
--

INSERT INTO `carrinho` (`id`, `carrinho`, `endereco`, `Pagamento`) VALUES
(37, '[{\"id\": 37, \"cor\": \"PRETO / FOSCO\", \"img\": \"https://g4motocenter.com.br/img/catalogo/CA4498/CA4498.jpg\", \"preco\": \"1.573,95\", \"idGrade\": 39, \"produto\": \"CAPACETE LS2 FF393 ESCAMOTEAVEL \", \"tamanho\": \"56\", \"referencia\": \"CA4498\", \"qtd_estoque\": \"1.00\"}, \"{\\\"id\\\":37,\\\"produto\\\":\\\"CAPACETE LS2 FF393 ESCAMOTEAVEL \\\",\\\"preco\\\":\\\"1.573,95\\\",\\\"img\\\":\\\"https://g4motocenter.com.br/img/catalogo/CA4496/CA4496.jpg\\\",\\\"qtd_estoque\\\":\\\"3.00\\\",\\\"idGrade\\\":41,\\\"cor\\\":\\\"PRETO\\\",\\\"tamanho\\\":\\\"60\\\",\\\"referencia\\\":\\\"CA4496\\\"}\"]', NULL, 'Pendente');

-- --------------------------------------------------------

--
-- Estrutura para tabela `compras`
--

CREATE TABLE `compras` (
  `IdCompra` int(40) NOT NULL,
  `Id` int(11) DEFAULT NULL,
  `UsuarioNome` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UsuarioCPF` varchar(14) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UsuarioEmail` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Produto` longtext COLLATE utf8mb4_unicode_ci,
  `RefProduto` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `MetodoPagamento` varchar(15) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Preco` float DEFAULT NULL,
  `endereco` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DataPedido` date NOT NULL,
  `Status` varchar(25) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Pendente'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estrutura para tabela `correios`
--

CREATE TABLE `correios` (
  `IdCompra` int(11) NOT NULL,
  `idLote` varchar(10) NOT NULL,
  `numeroCartaoPostagem` varchar(11) NOT NULL,
  `arquivo` varchar(40) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Despejando dados para a tabela `correios`
--

INSERT INTO `correios` (`IdCompra`, `idLote`, `numeroCartaoPostagem`, `arquivo`) VALUES
(387, 'TCUOI5198', '0078603030', 'PPN66de2680a18d2DT-20240909_003440.json'),
(389, 'OLXNH9473', '0078603030', 'PPN66de36754b115DT-20240909_014245.json'),
(390, 'IOEWK0046', '0078603030', 'PPN66de393364a43DT-20240909_015427.json'),
(391, 'IAQYD8703', '0078603030', 'PPN66df3e1eab3bdDT-20240909_202742.json'),
(392, 'DPOJE1398', '0078603030', 'PPN66df3e931682eDT-20240909_202939.json'),
(393, 'HGONJ3911', '0078603030', 'PPN66df4b98de4eeDT-20240909_212512.json'),
(394, 'CPTAT2654', '0078603030', 'PPN66df50117fa43DT-20240909_214417.json');

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto`
--

CREATE TABLE `produto` (
  `idProduto` int(11) NOT NULL,
  `codigoProduto` int(11) DEFAULT NULL,
  `grupoProduto` varchar(50) DEFAULT NULL,
  `referenciaProduto` varchar(50) DEFAULT NULL,
  `tituloEcommerceProduto` varchar(100) DEFAULT NULL,
  `descricaoConsultaProduto` text,
  `aplicacaoConsultaProduto` text,
  `dadosAdicionaisProduto` text,
  `precoTabelaProduto` decimal(10,3) DEFAULT NULL,
  `precoMinimoProduto` decimal(10,3) DEFAULT NULL,
  `qtdminProduto` decimal(10,2) DEFAULT NULL,
  `dataalt` datetime DEFAULT NULL,
  `eanProduto` varchar(50) DEFAULT NULL,
  `img` varchar(255) DEFAULT NULL,
  `pesoProduto` decimal(10,2) DEFAULT NULL,
  `alturaProduto` decimal(10,2) DEFAULT NULL,
  `larguraProduto` decimal(10,2) DEFAULT NULL,
  `comprimentoProduto` decimal(10,2) DEFAULT NULL,
  `fabricante` varchar(100) DEFAULT NULL,
  `corpredominanteProduto` varchar(50) DEFAULT NULL,
  `tamanhoProduto` varchar(50) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Despejando dados para a tabela `produto`
--

INSERT INTO `produto` (`idProduto`, `codigoProduto`, `grupoProduto`, `referenciaProduto`, `tituloEcommerceProduto`, `descricaoConsultaProduto`, `aplicacaoConsultaProduto`, `dadosAdicionaisProduto`, `precoTabelaProduto`, `precoMinimoProduto`, `qtdminProduto`, `dataalt`, `eanProduto`, `img`, `pesoProduto`, `alturaProduto`, `larguraProduto`, `comprimentoProduto`, `fabricante`, `corpredominanteProduto`, `tamanhoProduto`) VALUES
(11375, 29907, 'CAPACETE', 'CA7310', 'CAPACETE LS2 FF358 XDRON', 'O LS2 FF358 é um capacete ideal para o dia a dia, com diferentes gráficos para atender quaisquer estilos. Sua tecnologia tornou-se referência em cascos desenvolvidos em ABS, sendo classificado com 4 estrelas Sharp, pelo órgão britânico que adota os mais rigorosos padrões de segurança mundial. Tudo isso com ganhos de conforto, pois pesa apenas 1.400 gramas.', '', '', 699.010, 664.060, 0.00, '2025-10-29 11:45:23', '', 'https://g4motocenter.com.br/img/catalogo/CA7310/CA7310.jpg', 0.00, 0.00, 0.00, 0.00, 'LS2', 'LARANJA / BRANCO', '54'),
(12913, 30976, 'CAPACETE', 'CA7729', 'CAPACETE NORISK ROCK MONOCOLOR MATTE', '', '', '', 1099.010, 1044.050, 1.00, '2025-10-29 12:37:03', '', 'https://g4motocenter.com.br/img/catalogo/CA7729/CA7729.jpg', 0.00, 0.00, 0.00, 0.00, 'NORISK', 'PRETO / FOSCO', '58'),
(11922, 30377, 'CAPACETE', 'CA7515', 'CAPACETE RACE TECH VOLT TRACK BRILHOSO', '', '', 'COM VISEIRA SOLAR', 599.000, 569.050, 1.00, '2025-10-29 12:37:46', '7908167896838', 'https://g4motocenter.com.br/img/catalogo/CA7515/CA7515.jpg', 0.00, 0.00, 0.00, 0.00, 'RACE TECH', 'AZUL / BRANCO', '56'),
(9898, 26995, 'CAPACETE', 'CA7207', 'CAPACETE KYT TT COURSE 98 BOMB', '', '', '', 799.000, 759.050, 0.00, '2025-10-29 12:38:03', '', 'https://g4motocenter.com.br/img/catalogo/CA7207/CA7207.jpg', 0.00, 0.00, 0.00, 0.00, 'KYT', 'BRANCO / AMARELO', '56'),
(10422, 28959, 'CAPACETE', 'CA7018', 'CAPACETE PEELS CLICK CLASSIC', '', '', 'ABERTO', 399.000, 379.050, 0.00, '2025-10-29 12:38:19', '7899010527132', 'https://g4motocenter.com.br/img/catalogo/CA7018/CA7018.jpg', 0.00, 0.00, 0.00, 0.00, 'PEELS', 'PRETO / GRAFITE', '56'),
(11863, 28584, 'JAQUETA', 'JA1137', 'JAQUETA X-11 ONE SPORT', 'A jaqueta ideal para quem procura equipamentos básicos e com visual esportivo, leves e que garantem a segurança na pilotagem do dia a dia e nas estradas.  ', '', '', 649.000, 612.330, 2.00, '2025-10-29 12:38:47', '7899651868298', 'https://g4motocenter.com.br/img/catalogo/JA1137/JA1137.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'PRETO /', '3G'),
(4799, 29496, 'JAQUETA', 'JA1182', 'JAQUETA TEXX ARMOR MASCULINA', '', '', 'PARCA', 799.000, 759.050, 0.00, '2025-10-29 12:39:09', '7909201032250', 'https://g4motocenter.com.br/img/catalogo/JA1182/JA1182.jpg', 0.00, 0.00, 0.00, 0.00, 'TEXX', 'CINZA / PRETO', '2XL'),
(11961, 27696, 'JAQUETA', 'JA1049', 'JAQUETA X-11 IRON 3 IMPERMEAVEL ', '', '', '', 899.000, 850.660, 0.00, '2025-10-29 12:39:26', '7899651859470', 'https://g4motocenter.com.br/img/catalogo/JA1049/JA1049.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'VERMELHO / PRETO', 'GGGGGG (6XL)'),
(5098, 30837, 'JAQUETA', 'JA1246', 'JAQUETA TEXX NEW STRIKE V3 MASCULINA', '', '', 'IMPERMEAVEL', 598.980, 569.050, 0.00, '2025-10-29 12:39:46', '', 'https://g4motocenter.com.br/img/catalogo/JA1246/JA1246.jpg', 0.00, 0.00, 0.00, 0.00, 'TEXX', 'VERMELHO / PRETO', '2G'),
(1647, 25424, 'JAQUETA', 'JA891', 'JAQUETA X-11 GUARD 2', '', '', '', 556.000, 527.680, 0.00, '2025-10-29 12:40:04', '7899651832053', 'https://g4motocenter.com.br/img/catalogo/JA891/JA891.jpg', 0.00, 0.00, 0.00, 0.00, '-1', 'PRETO /', 'G'),
(4276, 17832, 'LUVA', 'LU314', 'LUVA X-11 BLACKOUT', 'A Blackout 2 traz um novo protetor de articulação, mais moderno e com melhor performance, mantendo o DNA que revolucionou o mercado de luvas para motociclistas do Brasil.', '', '', 105.000, 99.750, 14.00, '2025-10-29 12:40:22', '7899651858466', 'https://g4motocenter.com.br/img/catalogo/LU314/LU314.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'PRETO', 'G'),
(11011, 29513, 'LUVA', 'LU661', 'LUVA TEXX LIBERTY - COURO', '', '', '', 169.000, 160.550, 0.00, '2025-10-29 12:40:44', '7909201032625', 'https://g4motocenter.com.br/img/catalogo/LU661/LU661.jpg', 0.00, 0.00, 0.00, 0.00, 'TEXX', 'PRETO', '2XL'),
(7452, 22702, 'LUVA', 'LU438', 'LUVA X-11 FIT X', '', '', '', 83.900, 79.710, 5.00, '2025-10-29 12:45:26', '7899651816190', 'https://g4motocenter.com.br/img/catalogo/LU438/LU438.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'PRETO', 'G'),
(4421, 18604, 'LUVA', 'LU334', 'LUVA X-11 BLACKOUT 2 **MEIO DEDO**', '', '', '', 95.000, 89.680, 2.00, '2025-10-29 12:45:47', '7899651858558', 'https://g4motocenter.com.br/img/catalogo/LU334/LU334.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'PRETO', 'G'),
(11704, 27537, 'LUVA', 'LU566', 'LUVA X-11 BLACKPROOF IMPERMEAVEL', '', '', '', 209.900, 199.410, 2.00, '2025-10-29 12:46:03', '7899651849488', 'https://g4motocenter.com.br/img/catalogo/LU566/LU566.jpg', 0.00, 0.00, 0.00, 0.00, 'X-11', 'PRETO', 'G'),
(864, 3375, 'CONJUNTO CHUVA', 'CO011', 'CONJUNTO DE CHUVA ALBA EUROPA', 'A capa de chuva impermeável da Linha Europa é a mais tradicional da Alba. O seu tecido combina flexibilidade com resistência, proporcionando liberdade de movimentos e segurança nas manobras. Prático e seguro, possui um bolso externo, além de refletivos nas costas, manga direita e bolso dianteiro.', '', 'FORRADO', 169.990, 161.490, 91.00, '2025-10-29 12:47:46', '7898911482236', 'https://g4motocenter.com.br/img/catalogo/CO011/CO011.jpg', 0.00, 0.00, 0.00, 0.00, 'ALBA', 'PRETO', 'G'),
(2478, 18212, 'CONJUNTO CHUVA', 'CO682', 'CONJUNTO CHUVA ALBANY NYLON', '', '', '', 255.000, 242.260, 9.00, '2025-10-29 12:48:09', '7898911482502', 'https://g4motocenter.com.br/img/catalogo/CO682/CO682.jpg', 0.00, 0.00, 0.00, 0.00, 'ALBA', 'PRETO', 'G'),
(3835, 27101, 'CONJUNTO CHUVA', 'CO806', 'CONJUNTO CHUVA PIONEIRA COMBATE', '', '', '', 99.000, 94.050, 22.00, '2025-10-29 12:48:23', '7898712119591', 'https://g4motocenter.com.br/img/catalogo/CO806/CO806.jpg', 0.00, 0.00, 0.00, 0.00, 'PIONEIRA', 'PRETO', 'G'),
(1499, 8991, 'CONJUNTO CHUVA', 'CO0499', 'CONJUNTO CHUVA PIONEIRA COMBATE FEM.', '', '', 'FORRADO', 99.000, 94.060, 4.00, '2025-10-29 12:48:40', '', 'https://g4motocenter.com.br/img/catalogo/CO0499/CO0499.jpg', 0.00, 0.00, 0.00, 0.00, 'PIONEIRA', 'PRETO /', 'G'),
(11415, 30083, 'CONJUNTO CHUVA', 'CO839', 'CONJUNTO CHUVA GIVI', '', '', '', 469.000, 445.550, 0.00, '2025-10-29 12:48:52', '', 'https://g4motocenter.com.br/img/catalogo/CO839/CO839.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'VERMELHO', 'XL'),
(1523, 6031, 'BAU', 'BA001', 'BAU CARGA 90 LITROS QUADRADO', '', '', '45CM ALT./47CM LARG./47CM COMP.', 235.000, 223.250, 0.00, '2025-10-29 12:49:02', '9905110', 'https://g4motocenter.com.br/img/catalogo/BA001/BA001.jpg', 0.00, 0.00, 0.00, 0.00, 'RESIPLASTIC', 'BRANCO', ''),
(2649, 11443, 'BAU', 'BA0375', 'BAU CARGA 90 LITROS MAX (REDONDO)', '', '', '53CM ALT./52CM LARG./47CM COMP.', 379.000, 360.050, 0.00, '2025-10-29 12:49:19', '0744004', 'https://g4motocenter.com.br/img/catalogo/BA0375/BA0375.jpg', 0.00, 0.00, 0.00, 0.00, 'RESIPLASTIC', 'BRANCO /', ''),
(840, 20732, 'BAU', 'BA602', 'BAU CARGA 125 LITROS QUADRADO', '', '', '', 449.000, 379.060, 3.00, '2025-10-29 12:49:29', '', 'https://g4motocenter.com.br/img/catalogo/BA602/BA602.jpg', 0.00, 0.00, 0.00, 0.00, 'RESIPLASTIC', 'PRETO', ''),
(12837, 30733, 'BAULETO', 'BA871', 'BAULETO 33 LITROS GIVI', '\r\n- Baú Top Case Monokey;\r\n\r\n\r\n- Modelo E33N Point;\r\n\r\n\r\n- Compatibilidade Universal;\r\n\r\n\r\n- Qualidade Reconhecida Givi;\r\n\r\n\r\n\r\n\r\n\r\ndo Sobre o Bagageiro (Suporte) da Moto;\r\n\r\n\r\n- Acompanha Kit de Fixação Universal;\r\n\r\n\r\n- Capacidade de 33 Litros;\r\n\r\n\r\n- Suporta Até 3Kg de Carga;\r\n\r\n\r\n\r\n\r\n- Design Esportivo ', '', '', 429.000, 407.550, 0.00, '2025-10-29 12:49:52', '', 'https://g4motocenter.com.br/img/catalogo/BA871/BA871.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'FUME', ''),
(12415, 30709, 'BAULETO', 'BA870', 'BAULETO 47L GIVI', '', '', '', 739.000, 702.050, 4.00, '2025-10-29 12:50:04', '', 'https://g4motocenter.com.br/img/catalogo/BA870/BA870.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'FUME', ''),
(12396, 30708, 'BAULETO', 'BA869', 'BAULETO 45L GIVI', '', '', 'MONOLOCK SIMPLY', 599.000, 569.050, 3.00, '2025-10-29 12:50:17', '', 'https://g4motocenter.com.br/img/catalogo/BA869/BA869.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'FUME', ''),
(6027, 29892, 'BAULETO', 'BA851', 'BAULETO 46L RIVIERA MONOLOCK', '', '', '', 1399.000, 1329.050, 2.00, '2025-10-29 12:50:30', 'E46NTBR', 'https://g4motocenter.com.br/img/catalogo/BA851/BA851.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'FUME', ''),
(5770, 22213, 'BAULETO', 'BA670', 'BAULETO 58L OUTBACK TRASEIRO', '', '', 'SEM BASE( VENDER BASE MONOKEY)', 3699.000, 3580.550, 2.00, '2025-10-29 12:50:42', 'OBKN58A-BR', 'https://g4motocenter.com.br/img/catalogo/BA670/BA670.jpg', 0.00, 0.00, 0.00, 0.00, 'GIVI', 'ALUMINIO', ''),
(7315, 22968, 'BOTA CHUVA', 'BO877', 'BOTA CHUVA MOTOSAFE', '', '', 'FORRADA', 94.600, 89.870, 5.00, '2025-10-29 12:50:58', '', 'https://g4motocenter.com.br/img/catalogo/BO877/BO877.jpg', 0.00, 0.00, 0.00, 0.00, 'MOTOSAFE', 'PRETO / VERMELHO', '37');

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto_categoria`
--

CREATE TABLE `produto_categoria` (
  `idProdutoCategoria` int(11) NOT NULL,
  `idProduto` int(11) DEFAULT NULL,
  `descricaoCategoria` varchar(100) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto_especificacoes`
--

CREATE TABLE `produto_especificacoes` (
  `idProdutoEspecificacoes` int(11) NOT NULL,
  `idProduto` int(11) DEFAULT NULL,
  `tituloProdutoEspecificacoes` varchar(100) DEFAULT NULL,
  `DescProdutoEspecificacoes` varchar(100) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto_grade`
--

CREATE TABLE `produto_grade` (
  `idProdutoGrade` int(11) NOT NULL,
  `idProduto` int(11) DEFAULT NULL,
  `idProdutoPrinc` int(11) DEFAULT NULL,
  `referencia_produto` varchar(50) DEFAULT NULL,
  `corpredominante_produto` varchar(50) DEFAULT NULL,
  `tamanho_produto` varchar(20) DEFAULT NULL,
  `qtd_produto` decimal(10,2) DEFAULT NULL,
  `img` varchar(255) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Despejando dados para a tabela `produto_grade`
--

INSERT INTO `produto_grade` (`idProdutoGrade`, `idProduto`, `idProdutoPrinc`, `referencia_produto`, `corpredominante_produto`, `tamanho_produto`, `qtd_produto`, `img`) VALUES
(76, 11375, 11375, 'CA7310', 'LARANJA / BRANCO', '54', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7310/CA7310.jpg'),
(77, 13076, 11375, 'CA7718', 'VERMELHO / PRETO', '54', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7718/CA7718.jpg'),
(78, 11351, 11375, 'CA6929', 'AZUL', '56', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6929/CA6929.jpg'),
(79, 10551, 11375, 'CA6548', 'LARANJA / BRANCO', '56', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6548/CA6548.jpg'),
(80, 5575, 11375, 'CA7365', 'ROSA / BRANCO', '56', 3.00, 'https://g4motocenter.com.br/img/catalogo/CA7365/CA7365.jpg'),
(81, 13121, 11375, 'CA7728', 'VERMELHO / PRETO', '56', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7728/CA7728.jpg'),
(82, 11854, 11375, 'CA6930', 'AZUL', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6930/CA6930.jpg'),
(83, 9979, 11375, 'CA6414', 'LARANJA / BRANCO', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6414/CA6414.jpg'),
(84, 5581, 11375, 'CA7366', 'ROSA / BRANCO', '58', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7366/CA7366.jpg'),
(85, 10851, 11375, 'CA7224', 'ROSA / PRETO', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7224/CA7224.jpg'),
(86, 13077, 11375, 'CA7719', 'VERMELHO / PRETO', '58', 5.00, 'https://g4motocenter.com.br/img/catalogo/CA7719/CA7719.jpg'),
(87, 10578, 11375, 'CA7222', 'AZUL', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7222/CA7222.jpg'),
(88, 9980, 11375, 'CA6415', 'LARANJA / BRANCO', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6415/CA6415.jpg'),
(89, 11206, 11375, 'CA7262', 'ROSA / BRANCO', '60', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7262/CA7262.jpg'),
(90, 13078, 11375, 'CA7720', 'VERMELHO / PRETO', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7720/CA7720.jpg'),
(91, 11135, 11375, 'CA7230', 'AZUL', '62', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7230/CA7230.jpg'),
(92, 9981, 11375, 'CA6416', 'LARANJA / BRANCO', '62', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA6416/CA6416.jpg'),
(93, 4623, 11375, 'CA3838', 'ROSA / BRANCO', '62', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA3838/CA3838.jpg'),
(94, 13079, 11375, 'CA7721', 'VERMELHO / PRETO', '62', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7721/CA7721.jpg'),
(95, 12517, 11375, 'CA7487', 'ROSA / PRETO', '54', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7487/CA7487.jpg'),
(96, 10830, 11375, 'CA7223', 'ROSA / PRETO', '56', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7223/CA7223.jpg'),
(97, 12787, 11375, 'CA7454', 'ROSA / PRETO', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7454/CA7454.jpg'),
(98, 11545, 11375, 'CA7422', 'ROSA', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7422/CA7422.jpg'),
(99, 11549, 11375, 'CA7423', 'ROSA / PRETO', '62', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7423/CA7423.jpg'),
(100, 12913, 12913, 'CA7729', 'PRETO / FOSCO', '58', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7729/CA7729.jpg'),
(101, 12914, 12913, 'CA7730', 'PRETO / FOSCO', '60', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7730/CA7730.jpg'),
(102, 12915, 12913, 'CA7731', 'PRETO / FOSCO', '62', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7731/CA7731.jpg'),
(103, 0, 11922, '', '', '', 0.00, 'https://g4motocenter.com.br/img/catalogo//.jpg'),
(104, 11922, 11922, 'CA7515', 'AZUL / BRANCO', '56', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7515/CA7515.jpg'),
(106, 12843, 11922, 'CA7651', 'ROSA / BRANCO', '56', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7651/CA7651.jpg'),
(107, 11926, 11922, 'CA7516', 'AZUL / BRANCO', '58', 3.00, 'https://g4motocenter.com.br/img/catalogo/CA7516/CA7516.jpg'),
(108, 12844, 11922, 'CA7652', 'ROSA /', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7652/CA7652.jpg'),
(109, 11929, 11922, 'CA7517', 'AZUL / BRANCO', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7517/CA7517.jpg'),
(110, 11936, 11922, 'CA7518', 'AZUL / BRANCO', '62', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7518/CA7518.jpg'),
(111, 9898, 9898, 'CA7207', 'BRANCO / AMARELO', '56', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7207/CA7207.jpg'),
(113, 11210, 9898, 'CA7198', 'BRANCO / AMARELO', '58', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7198/CA7198.jpg'),
(114, 11214, 9898, 'CA7199', 'BRANCO / AMARELO', '60', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7199/CA7199.jpg'),
(115, 11234, 9898, 'CA7205', 'BRANCO / AMARELO', '62', 1.00, 'https://g4motocenter.com.br/img/catalogo/CA7205/CA7205.jpg'),
(116, 10422, 10422, 'CA7018', 'PRETO / GRAFITE', '56', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7018/CA7018.jpg'),
(117, 10439, 10422, 'CA7019', 'PRETO / GRAFITE', '58', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7019/CA7019.jpg'),
(118, 10442, 10422, 'CA7020', 'PRETO / GRAFITE', '60', 2.00, 'https://g4motocenter.com.br/img/catalogo/CA7020/CA7020.jpg'),
(119, 10451, 10422, 'CA7021', 'PRETO / GRAFITE', '62', 0.00, 'https://g4motocenter.com.br/img/catalogo/CA7021/CA7021.jpg'),
(120, 11863, 11863, 'JA1137', 'PRETO /', '3G', 2.00, 'https://g4motocenter.com.br/img/catalogo/JA1137/JA1137.jpg'),
(121, 11862, 11863, 'JA1136', 'PRETO', '4G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1136/JA1136.jpg'),
(122, 11694, 11863, 'JA1155', 'PRETO', '5G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1155/JA1155.jpg'),
(123, 11695, 11863, 'JA1156', 'PRETO', '6G', 2.00, 'https://g4motocenter.com.br/img/catalogo/JA1156/JA1156.jpg'),
(124, 10618, 11863, 'JA1163', 'PRETO / VERMELHO', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1163/JA1163.jpg'),
(125, 11859, 11863, 'JA1134', 'PRETO', 'G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1134/JA1134.jpg'),
(126, 10631, 11863, 'JA1164', 'PRETO / VERMELHO', 'GG', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1164/JA1164.jpg'),
(127, 11860, 11863, 'JA1135', 'PRETO', 'GG', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1135/JA1135.jpg'),
(128, 11858, 11863, 'JA1133', 'PRETO', 'M', 2.00, 'https://g4motocenter.com.br/img/catalogo/JA1133/JA1133.jpg'),
(129, 11693, 11863, 'JA1154', 'PRETO', 'P', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1154/JA1154.jpg'),
(130, 4799, 4799, 'JA1182', 'CINZA / PRETO', '2XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1182/JA1182.jpg'),
(131, 4801, 4799, 'JA1183', 'CINZA / PRETO', '3XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1183/JA1183.jpg'),
(132, 12541, 4799, 'JA1242', 'PRETO', '3XL', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1242/JA1242.jpg'),
(133, 4804, 4799, 'JA1184', 'CINZA / PRETO', '4XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1184/JA1184.jpg'),
(134, 4797, 4799, 'JA1180', 'CINZA / PRETO', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1180/JA1180.jpg'),
(135, 4798, 4799, 'JA1181', 'CINZA / PRETO', 'GG', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1181/JA1181.jpg'),
(136, 12524, 4799, 'JA1240', 'PRETO', 'L', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1240/JA1240.jpg'),
(137, 12525, 4799, 'JA1241', 'PRETO', 'XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1241/JA1241.jpg'),
(138, 11961, 11961, 'JA1049', 'VERMELHO / PRETO', 'GGGGGG (6XL)', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1049/JA1049.jpg'),
(139, 11098, 11961, 'JA1108', 'PRETO', '4XL', 3.00, 'https://g4motocenter.com.br/img/catalogo/JA1108/JA1108.jpg'),
(140, 11960, 11961, 'JA1048', 'PRETO / VERMELHO', '5XL', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1048/JA1048.jpg'),
(141, 10403, 11961, 'JA1089', 'PRETO', 'G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1089/JA1089.jpg'),
(142, 11951, 11961, 'JA1044', 'VERMELHO / PRETO', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1044/JA1044.jpg'),
(143, 11342, 11961, 'JA1091', 'PRETO', 'GG', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1091/JA1091.jpg'),
(144, 11953, 11961, 'JA1045', 'VERMELHO / PRETO', 'GG', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1045/JA1045.jpg'),
(145, 11955, 11961, 'JA1046', 'PRETO /', 'GGG', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1046/JA1046.jpg'),
(146, 11957, 11961, 'JA1047', 'VERMELHO / PRETO', 'GGGG (4XL)', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1047/JA1047.jpg'),
(147, 10401, 11961, 'JA1088', 'PRETO', 'M', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1088/JA1088.jpg'),
(148, 11950, 11961, 'JA1043', 'VERMELHO / PRETO', 'M', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1043/JA1043.jpg'),
(149, 10397, 11961, 'JA1087', 'PRETO', 'P', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1087/JA1087.jpg'),
(150, 11716, 11961, 'JA1054', 'PRETO', 'P', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1054/JA1054.jpg'),
(151, 5098, 5098, 'JA1246', 'VERMELHO / PRETO', '2G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1246/JA1246.jpg'),
(152, 5099, 5098, 'JA1247', 'VERMELHO / PRETO', '3G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1247/JA1247.jpg'),
(153, 5100, 5098, 'JA1248', 'VERMELHO / PRETO', '4G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1248/JA1248.jpg'),
(154, 2514, 5098, 'JA1249', 'VERMELHO / PRETO', '5G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1249/JA1249.jpg'),
(155, 2540, 5098, 'JA1250', 'VERMELHO / PRETO', '6G', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA1250/JA1250.jpg'),
(156, 12697, 5098, 'JA1251', 'VERMELHO / PRETO', '7G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1251/JA1251.jpg'),
(157, 4909, 5098, 'JA1245', 'VERMELHO / PRETO', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1245/JA1245.jpg'),
(158, 4904, 5098, 'JA1244', 'VERMELHO / PRETO', 'M', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA1244/JA1244.jpg'),
(159, 1647, 1647, 'JA891', 'PRETO /', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA891/JA891.jpg'),
(160, 6654, 1647, 'JA895', 'PRETO', 'GG', 0.00, 'https://g4motocenter.com.br/img/catalogo/JA895/JA895.jpg'),
(161, 2422, 1647, 'JA893', 'PRETO', 'GGG', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA893/JA893.jpg'),
(162, 2445, 1647, 'JA894', 'PRETO', 'GGGG (4XL)', 2.00, 'https://g4motocenter.com.br/img/catalogo/JA894/JA894.jpg'),
(163, 2060, 1647, 'JA892', 'PRETO', 'M', 1.00, 'https://g4motocenter.com.br/img/catalogo/JA892/JA892.jpg'),
(164, 11668, 1647, 'JA1109', 'PRETO', 'P', 2.00, 'https://g4motocenter.com.br/img/catalogo/JA1109/JA1109.jpg'),
(165, 4276, 4276, 'LU314', 'PRETO', 'G', 14.00, 'https://g4motocenter.com.br/img/catalogo/LU314/LU314.jpg'),
(166, 10208, 4276, 'LU556', 'PRETO', 'GG', 16.00, 'https://g4motocenter.com.br/img/catalogo/LU556/LU556.jpg'),
(167, 10210, 4276, 'LU557', 'PRETO', 'GGG', 8.00, 'https://g4motocenter.com.br/img/catalogo/LU557/LU557.jpg'),
(168, 10204, 4276, 'LU554', 'PRETO', 'M', 4.00, 'https://g4motocenter.com.br/img/catalogo/LU554/LU554.jpg'),
(169, 10201, 4276, 'LU553', 'PRETO', 'P', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU553/LU553.jpg'),
(170, 11011, 11011, 'LU661', 'PRETO', '2XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU661/LU661.jpg'),
(171, 12031, 11011, 'LU585', 'PRETO', '3XL', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU585/LU585.jpg'),
(172, 4806, 11011, 'LU649', 'PRETO', '4XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU649/LU649.jpg'),
(173, 12026, 11011, 'LU583', 'PRETO', 'G', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU583/LU583.jpg'),
(174, 12029, 11011, 'LU584', 'PRETO', 'GG', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU584/LU584.jpg'),
(175, 12025, 11011, 'LU582', 'PRETO', 'M', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU582/LU582.jpg'),
(176, 12020, 11011, 'LU581', 'PRETO', 'P', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU581/LU581.jpg'),
(177, 0, 7452, ' ', '', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/ / .jpg'),
(178, 7452, 7452, 'LU438', 'PRETO', 'G', 5.00, 'https://g4motocenter.com.br/img/catalogo/LU438/LU438.jpg'),
(179, 7450, 7452, 'LU436', 'PRETO', 'GG', 7.00, 'https://g4motocenter.com.br/img/catalogo/LU436/LU436.jpg'),
(180, 6757, 7452, 'LU484', 'PRETO', 'GGG', 10.00, 'https://g4motocenter.com.br/img/catalogo/LU484/LU484.jpg'),
(181, 7451, 7452, 'LU437', 'PRETO', 'M', 8.00, 'https://g4motocenter.com.br/img/catalogo/LU437/LU437.jpg'),
(182, 7453, 7452, 'LU439', 'PRETO', 'P', 4.00, 'https://g4motocenter.com.br/img/catalogo/LU439/LU439.jpg'),
(183, 4421, 4421, 'LU334', 'PRETO', 'G', 2.00, 'https://g4motocenter.com.br/img/catalogo/LU334/LU334.jpg'),
(184, 4422, 4421, 'LU335', 'PRETO', 'GG', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU335/LU335.jpg'),
(185, 7023, 4421, 'LU506', 'PRETO', 'GGG', 0.00, 'https://g4motocenter.com.br/img/catalogo/LU506/LU506.jpg'),
(186, 4420, 4421, 'LU333', 'PRETO', 'M', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU333/LU333.jpg'),
(187, 4419, 4421, 'LU332', 'PRETO', 'P', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU332/LU332.jpg'),
(188, 11704, 11704, 'LU566', 'PRETO', 'G', 2.00, 'https://g4motocenter.com.br/img/catalogo/LU566/LU566.jpg'),
(189, 11702, 11704, 'LU564', 'PRETO', 'GG', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU564/LU564.jpg'),
(190, 11703, 11704, 'LU565', 'PRETO', 'GGG', 2.00, 'https://g4motocenter.com.br/img/catalogo/LU565/LU565.jpg'),
(191, 11705, 11704, 'LU567', 'PRETO', 'M', 2.00, 'https://g4motocenter.com.br/img/catalogo/LU567/LU567.jpg'),
(192, 11706, 11704, 'LU568', 'PRETO', 'P', 1.00, 'https://g4motocenter.com.br/img/catalogo/LU568/LU568.jpg'),
(193, 864, 864, 'CO011', 'PRETO', 'G', 91.00, 'https://g4motocenter.com.br/img/catalogo/CO011/CO011.jpg'),
(194, 866, 864, 'CO023', 'PRETO', 'GG', 42.00, 'https://g4motocenter.com.br/img/catalogo/CO023/CO023.jpg'),
(195, 862, 864, 'CO006', 'PRETO', 'GGG', 9.00, 'https://g4motocenter.com.br/img/catalogo/CO006/CO006.jpg'),
(196, 863, 864, 'CO007', 'PRETO /', 'M', 41.00, 'https://g4motocenter.com.br/img/catalogo/CO007/CO007.jpg'),
(197, 3427, 864, 'CO597', 'PRETO', 'P', 14.00, 'https://g4motocenter.com.br/img/catalogo/CO597/CO597.jpg'),
(198, 2478, 2478, 'CO682', 'PRETO', 'G', 9.00, 'https://g4motocenter.com.br/img/catalogo/CO682/CO682.jpg'),
(199, 2703, 2478, 'CO0547', 'PRETO', 'GG', 11.00, 'https://g4motocenter.com.br/img/catalogo/CO0547/CO0547.jpg'),
(200, 5190, 2478, 'CO728', 'PRETO', 'GGG', 7.00, 'https://g4motocenter.com.br/img/catalogo/CO728/CO728.jpg'),
(201, 2671, 2478, 'CO683', 'PRETO', 'M', 6.00, 'https://g4motocenter.com.br/img/catalogo/CO683/CO683.jpg'),
(202, 3285, 2478, 'CO684', 'PRETO', 'P', 2.00, 'https://g4motocenter.com.br/img/catalogo/CO684/CO684.jpg'),
(203, 3835, 3835, 'CO806', 'PRETO', 'G', 22.00, 'https://g4motocenter.com.br/img/catalogo/CO806/CO806.jpg'),
(204, 3836, 3835, 'CO807', 'PRETO', 'GG', 44.00, 'https://g4motocenter.com.br/img/catalogo/CO807/CO807.jpg'),
(205, 4214, 3835, 'CO808', 'PRETO', 'GGG', 17.00, 'https://g4motocenter.com.br/img/catalogo/CO808/CO808.jpg'),
(206, 3565, 3835, 'CO805', 'PRETO', 'M', 45.00, 'https://g4motocenter.com.br/img/catalogo/CO805/CO805.jpg'),
(207, 3546, 3835, 'CO804', 'PRETO', 'P', 17.00, 'https://g4motocenter.com.br/img/catalogo/CO804/CO804.jpg'),
(214, 1499, 1499, 'CO0499', 'PRETO /', 'G', 4.00, 'https://g4motocenter.com.br/img/catalogo/CO0499/CO0499.jpg'),
(215, 1380, 1499, 'CO0498', 'PRETO /', 'M', 2.00, 'https://g4motocenter.com.br/img/catalogo/CO0498/CO0498.jpg'),
(216, 1181, 1499, 'CO0497', 'PRETO /', 'P', 4.00, 'https://g4motocenter.com.br/img/catalogo/CO0497/CO0497.jpg'),
(217, 11415, 11415, 'CO839', 'VERMELHO', 'XL', 0.00, 'https://g4motocenter.com.br/img/catalogo/CO839/CO839.jpg'),
(218, 11748, 11415, 'CO838', 'VERMELHO', 'XXL', 0.00, 'https://g4motocenter.com.br/img/catalogo/CO838/CO838.jpg'),
(247, 1523, 1523, 'BA001', 'BRANCO', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/BA001/BA001.jpg'),
(248, 2067, 1523, 'BA0330', 'PRETO', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/BA0330/BA0330.jpg'),
(249, 2649, 2649, 'BA0375', 'BRANCO /', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/BA0375/BA0375.jpg'),
(250, 2647, 2649, 'BA0374', 'PRETO', '', 18.00, 'https://g4motocenter.com.br/img/catalogo/BA0374/BA0374.jpg'),
(253, 0, 12837, ' ', '', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/ / .jpg'),
(254, 12837, 12837, 'BA871', 'FUME', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/BA871/BA871.jpg'),
(255, 1713, 12837, 'BA065', 'VERMELHO', '', 4.00, 'https://g4motocenter.com.br/img/catalogo/BA065/BA065.jpg'),
(256, 12415, 12415, 'BA870', 'FUME', '', 4.00, 'https://g4motocenter.com.br/img/catalogo/BA870/BA870.jpg'),
(257, 2945, 12415, 'BA453', 'VERMELHO', '', 5.00, 'https://g4motocenter.com.br/img/catalogo/BA453/BA453.jpg'),
(258, 12396, 12396, 'BA869', 'FUME', '', 3.00, 'https://g4motocenter.com.br/img/catalogo/BA869/BA869.jpg'),
(259, 1062, 12396, 'BA129', 'VERMELHO', '', 6.00, 'https://g4motocenter.com.br/img/catalogo/BA129/BA129.jpg'),
(251, 6027, 6027, 'BA851', 'FUME', '', 2.00, 'https://g4motocenter.com.br/img/catalogo/BA851/BA851.jpg'),
(252, 6018, 6027, 'BA850', 'VERMELHO', '', 0.00, 'https://g4motocenter.com.br/img/catalogo/BA850/BA850.jpg'),
(260, 5770, 5770, 'BA670', 'ALUMINIO', '', 2.00, 'https://g4motocenter.com.br/img/catalogo/BA670/BA670.jpg'),
(261, 6031, 5770, 'BA668', 'PRETO', '', 2.00, 'https://g4motocenter.com.br/img/catalogo/BA668/BA668.jpg'),
(262, 7315, 7315, 'BO877', 'PRETO / VERMELHO', '37', 5.00, 'https://g4motocenter.com.br/img/catalogo/BO877/BO877.jpg'),
(263, 7324, 7315, 'BO885', 'PRETO', '37', 6.00, 'https://g4motocenter.com.br/img/catalogo/BO885/BO885.jpg'),
(264, 7316, 7315, 'BO878', 'PRETO / VERMELHO', '38', 12.00, 'https://g4motocenter.com.br/img/catalogo/BO878/BO878.jpg'),
(265, 7325, 7315, 'BO886', 'PRETO', '38', 9.00, 'https://g4motocenter.com.br/img/catalogo/BO886/BO886.jpg'),
(266, 7318, 7315, 'BO879', 'PRETO / VERMELHO', '39', 8.00, 'https://g4motocenter.com.br/img/catalogo/BO879/BO879.jpg'),
(267, 7326, 7315, 'BO887', 'PRETO', '39', 14.00, 'https://g4motocenter.com.br/img/catalogo/BO887/BO887.jpg'),
(268, 7319, 7315, 'BO880', 'PRETO / VERMELHO', '40', 17.00, 'https://g4motocenter.com.br/img/catalogo/BO880/BO880.jpg'),
(269, 7327, 7315, 'BO888', 'PRETO', '40', 14.00, 'https://g4motocenter.com.br/img/catalogo/BO888/BO888.jpg'),
(270, 7320, 7315, 'BO881', 'PRETO / VERMELHO', '41', 23.00, 'https://g4motocenter.com.br/img/catalogo/BO881/BO881.jpg'),
(271, 7328, 7315, 'BO889', 'PRETO', '41', 23.00, 'https://g4motocenter.com.br/img/catalogo/BO889/BO889.jpg'),
(272, 7321, 7315, 'BO882', 'PRETO / VERMELHO', '42', 21.00, 'https://g4motocenter.com.br/img/catalogo/BO882/BO882.jpg'),
(273, 7329, 7315, 'BO890', 'PRETO', '42', 12.00, 'https://g4motocenter.com.br/img/catalogo/BO890/BO890.jpg'),
(274, 7322, 7315, 'BO883', 'PRETO / VERMELHO', '43', 13.00, 'https://g4motocenter.com.br/img/catalogo/BO883/BO883.jpg'),
(275, 7330, 7315, 'BO891', 'PRETO', '43', 10.00, 'https://g4motocenter.com.br/img/catalogo/BO891/BO891.jpg'),
(276, 7323, 7315, 'BO884', 'PRETO / VERMELHO', '44', 12.00, 'https://g4motocenter.com.br/img/catalogo/BO884/BO884.jpg'),
(277, 7331, 7315, 'BO892', 'PRETO', '44', 10.00, 'https://g4motocenter.com.br/img/catalogo/BO892/BO892.jpg');

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto_grupo`
--

CREATE TABLE `produto_grupo` (
  `idProdutoGrupo` int(11) NOT NULL,
  `descricaogrupoProduto` varchar(50) DEFAULT NULL,
  `img` varchar(255) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Despejando dados para a tabela `produto_grupo`
--

INSERT INTO `produto_grupo` (`idProdutoGrupo`, `descricaogrupoProduto`, `img`) VALUES
(2, 'Capacetes', 'https://g4motocenter.com.br/img/catalogogrupo/grupo_2.jpg'),
(3, 'Jaquetas', 'https://g4motocenter.com.br/img/catalogogrupo/grupo_3.jpg'),
(5, 'Para Você', 'https://g4motocenter.com.br/img/catalogogrupo/grupo_5.jpg'),
(1, 'Acessórios', 'https://g4motocenter.com.br/img/catalogogrupo/grupo_1.jpg');

-- --------------------------------------------------------

--
-- Estrutura para tabela `produto_sub_grupo`
--

CREATE TABLE `produto_sub_grupo` (
  `idProdutoSubGrupo` int(11) NOT NULL,
  `idProdutoGrupo` int(11) DEFAULT NULL,
  `descricaosubgrupoProduto` varchar(50) DEFAULT NULL,
  `img` varchar(255) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Despejando dados para a tabela `produto_sub_grupo`
--

INSERT INTO `produto_sub_grupo` (`idProdutoSubGrupo`, `idProdutoGrupo`, `descricaosubgrupoProduto`, `img`) VALUES
(3, 2, 'Capacete', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_3.jpg'),
(13, 3, 'Jaqueta', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_13.jpg'),
(14, 5, 'Luva', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_14.jpg'),
(15, 5, 'Conjunto de Chuva', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_15.jpg'),
(17, 1, 'Bau', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_17.jpg'),
(18, 1, 'Bauleto', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_18.jpg'),
(19, 5, 'Bota Chuva', 'https://g4motocenter.com.br/img/catalogogrupo/subgrupo_19.jpg');

--
-- Índices para tabelas despejadas
--

--
-- Índices de tabela `cadastros`
--
ALTER TABLE `cadastros`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `carrinho`
--
ALTER TABLE `carrinho`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `compras`
--
ALTER TABLE `compras`
  ADD PRIMARY KEY (`IdCompra`);

--
-- Índices de tabela `correios`
--
ALTER TABLE `correios`
  ADD UNIQUE KEY `IdCompra` (`IdCompra`);

--
-- Índices de tabela `produto`
--
ALTER TABLE `produto`
  ADD PRIMARY KEY (`idProduto`);

--
-- Índices de tabela `produto_categoria`
--
ALTER TABLE `produto_categoria`
  ADD PRIMARY KEY (`idProdutoCategoria`),
  ADD KEY `fk_categoria_produto` (`idProduto`);

--
-- Índices de tabela `produto_especificacoes`
--
ALTER TABLE `produto_especificacoes`
  ADD PRIMARY KEY (`idProdutoEspecificacoes`),
  ADD KEY `fk_especificacoes_produto` (`idProduto`);

--
-- Índices de tabela `produto_grade`
--
ALTER TABLE `produto_grade`
  ADD PRIMARY KEY (`idProdutoGrade`),
  ADD KEY `fk_grade_produto` (`idProdutoPrinc`);

--
-- Índices de tabela `produto_grupo`
--
ALTER TABLE `produto_grupo`
  ADD PRIMARY KEY (`idProdutoGrupo`);

--
-- Índices de tabela `produto_sub_grupo`
--
ALTER TABLE `produto_sub_grupo`
  ADD PRIMARY KEY (`idProdutoSubGrupo`),
  ADD KEY `fk_produto_sub_grupo` (`idProdutoGrupo`);

--
-- AUTO_INCREMENT para tabelas despejadas
--

--
-- AUTO_INCREMENT de tabela `cadastros`
--
ALTER TABLE `cadastros`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT de tabela `compras`
--
ALTER TABLE `compras`
  MODIFY `IdCompra` int(40) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de tabela `produto`
--
ALTER TABLE `produto`
  MODIFY `idProduto` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12914;

--
-- AUTO_INCREMENT de tabela `produto_categoria`
--
ALTER TABLE `produto_categoria`
  MODIFY `idProdutoCategoria` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de tabela `produto_especificacoes`
--
ALTER TABLE `produto_especificacoes`
  MODIFY `idProdutoEspecificacoes` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de tabela `produto_grade`
--
ALTER TABLE `produto_grade`
  MODIFY `idProdutoGrade` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=278;

--
-- AUTO_INCREMENT de tabela `produto_grupo`
--
ALTER TABLE `produto_grupo`
  MODIFY `idProdutoGrupo` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de tabela `produto_sub_grupo`
--
ALTER TABLE `produto_sub_grupo`
  MODIFY `idProdutoSubGrupo` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=20;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
