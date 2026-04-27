CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP TABLE IF EXISTS public.users CASCADE;
CREATE TABLE public.users (
    id          SERIAL PRIMARY KEY,
    first_name  VARCHAR(100) NOT NULL,
    last_name   VARCHAR(100) NOT NULL,
    email       VARCHAR(150) NOT NULL UNIQUE,
    password    VARCHAR(255) NOT NULL
);

DROP TABLE IF EXISTS public.transactions CASCADE;
CREATE TABLE public.transactions (
    id                SERIAL PRIMARY KEY,
    user_id           INTEGER REFERENCES public.users(id) ON DELETE CASCADE,
    crypto_symbol     VARCHAR(10),
    amount            NUMERIC,
    price_at_purchase NUMERIC,
    "timestamp"       TIMESTAMP,
    side              VARCHAR(4) NOT NULL DEFAULT 'Buy',
    trade_id          UUID NOT NULL DEFAULT gen_random_uuid()
);

DROP TABLE IF EXISTS public.trades CASCADE;
CREATE TABLE public.trades (
    id            SERIAL PRIMARY KEY,
    user_id       INTEGER NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    crypto_symbol VARCHAR(20) NOT NULL,
    buy_date      TIMESTAMP WITH TIME ZONE NOT NULL,
    buy_price     NUMERIC(20, 8) NOT NULL,
    quantity      NUMERIC(38, 18) NOT NULL,
    sell_date     TIMESTAMP WITH TIME ZONE,
    sell_price    NUMERIC(20, 8),
    status        VARCHAR(10) NOT NULL DEFAULT 'Open',
    stop_loss     NUMERIC(20, 8),
    take_profit   NUMERIC(20, 8)
);

CREATE INDEX idx_trades_buy_date ON public.trades (buy_date);
CREATE INDEX idx_trades_symbol   ON public.trades (crypto_symbol);
CREATE INDEX idx_trades_user_id  ON public.trades (user_id);

DROP TABLE IF EXISTS public.sentiment_scores CASCADE;
CREATE TABLE public.sentiment_scores (
    id        BIGSERIAL PRIMARY KEY,
    ts        TIMESTAMP WITH TIME ZONE NOT NULL,
    ts_hour   TIMESTAMP WITH TIME ZONE NOT NULL UNIQUE,
    score     DOUBLE PRECISION NOT NULL,
    price_btc DOUBLE PRECISION,
    price_eth DOUBLE PRECISION
);

CREATE INDEX idx_sentiment_scores_ts_hour ON public.sentiment_scores (ts_hour DESC);

DROP TABLE IF EXISTS public.sentiment_details CASCADE;
CREATE TABLE public.sentiment_details (
    ts_hour      TIMESTAMP WITH TIME ZONE PRIMARY KEY,
    json_payload JSONB NOT NULL DEFAULT '{}'::JSONB
);

DROP TABLE IF EXISTS public.reddit_raw CASCADE;
CREATE TABLE public.reddit_raw (
    id   TEXT PRIMARY KEY,
    ts   TIMESTAMP WITH TIME ZONE NOT NULL,
    sub  VARCHAR(100) NOT NULL,
    data JSONB NOT NULL
);

CREATE INDEX idx_reddit_raw_ts  ON public.reddit_raw (ts DESC);
CREATE INDEX idx_reddit_raw_sub ON public.reddit_raw (sub);
