Sql скрипт для создания базы данных для работы программы
CREATE TABLE IF NOT EXISTS external_data (
    id          BIGSERIAL PRIMARY KEY,
    api_name    VARCHAR(100) NOT NULL,
    response_json JSONB NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Индекс для быстрого поиска по названию API и дате
CREATE INDEX IF NOT EXISTS idx_external_data_api_created 
    ON external_data (api_name, created_at DESC);
