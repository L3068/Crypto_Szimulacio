use CrypoDb_FKMMEF

SET IDENTITY_INSERT CryptoCurrencies ON;
INSERT INTO CryptoCurrencies (Id, Name, Symbol, CurrentPrice, TotalSupply) VALUES
(1, 'Bitcoin', 'BTC', 64000, 21000000),
(2, 'Ethereum', 'ETH', 3200, 120000000),
(3, 'Litecoin', 'LTC', 85, 84000000),
(4, 'Dogecoin', 'DOGE', 0.25, 130000000000),
(5, 'Cardano', 'ADA', 1.2, 45000000000),
(6, 'Solana', 'SOL', 150, 500000000),
(7, 'Ripple', 'XRP', 0.6, 100000000000),
(8, 'Polkadot', 'DOT', 7, 1000000000),
(9, 'Chainlink', 'LINK', 15, 1000000000),
(10, 'Stellar', 'XLM', 0.1, 50000000000),
(11, 'Avalanche', 'AVAX', 40, 720000000),
(12, 'Uniswap', 'UNI', 6.5, 1000000000),
(13, 'VeChain', 'VET', 0.03, 86700000000),
(14, 'Tezos', 'XTZ', 1.5, 900000000),
(15, 'Cosmos', 'ATOM', 10, 300000000);
SET IDENTITY_INSERT CryptoCurrencies OFF;

SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, Username, Password, Email) VALUES
(1, 'alice', 'hashedpass1', 'alice@example.com'),
(2, 'bob', 'hashedpass2', 'bob@example.com'),
(3, 'charlie', 'hashedpass3', 'charlie@example.com');
SET IDENTITY_INSERT Users OFF;

SET IDENTITY_INSERT Wallets ON;
INSERT INTO Wallets (Id, UserId, Balance) VALUES
(1, 1, 15000),
(2, 2, 8000),
(3, 3, 5000);
SET IDENTITY_INSERT Wallets OFF;

INSERT INTO PortfolioItems (WalletId, CryptoId, Quantity, AveragePrice) VALUES
(1, 1, 0.2, 60000),
(1, 2, 5, 3000),
(2, 4, 10000, 0.2),
(2, 5, 500, 1),
(3, 6, 10, 100),
(3, 7, 1000, 0.5);

--

SET IDENTITY_INSERT Transactions ON;
INSERT INTO Transactions (Id, UserId, CryptoId, Type, Quantity, PricePerUnit, TotalPrice, Timestamp) VALUES
(1, 1, 1, 0, 0.2, 60000, 12000, '2025-04-01T10:00:00'),
(2, 1, 2, 1, 5, 3000, 15000, '2025-04-02T11:00:00'),
(3, 2, 4, 1, 10000, 0.2, 2000, '2025-04-03T12:00:00'),
(4, 2, 5, 0, 500, 1, 500, '2025-04-04T13:00:00'),
(5, 3, 6, 0, 10, 100, 1000, '2025-04-05T14:00:00'),
(6, 3, 7, 1, 1000, 0.5, 500, '2025-04-06T15:00:00');
SET IDENTITY_INSERT Transactions OFF;

SET IDENTITY_INSERT PriceHistories ON;
INSERT INTO PriceHistories (Id, CryptoId, Price, Timestamp) VALUES
(1, 1, 59000, '2025-03-30T10:00:00'),
(2, 1, 60000, '2025-04-01T10:00:00'),
(3, 2, 3100, '2025-03-30T11:00:00'),
(4, 2, 3000, '2025-04-02T11:00:00'),
(5, 4, 0.19, '2025-03-30T12:00:00'),
(6, 4, 0.2, '2025-04-03T12:00:00'),
(7, 5, 1.1, '2025-03-30T13:00:00'),
(8, 5, 1.2, '2025-04-04T13:00:00'),
(9, 6, 140, '2025-03-30T14:00:00'),
(10, 6, 150, '2025-04-05T14:00:00');
SET IDENTITY_INSERT PriceHistories OFF;
