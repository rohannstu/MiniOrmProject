-- up
CREATE TABLE orders (
    id          SERIAL PRIMARY KEY,
    product_id  INTEGER NOT NULL,
    quantity    INTEGER NOT NULL,
    total_price NUMERIC NOT NULL,
    note        TEXT NULL,
    ordered_at  TIMESTAMP NOT NULL
);

-- down
DROP TABLE orders;