-- up
CREATE TABLE products (
    id         SERIAL PRIMARY KEY,
    name       TEXT NOT NULL,
    price      NUMERIC NOT NULL,
    discount   NUMERIC NULL,
    in_stock   BOOLEAN NOT NULL,
    created_at TIMESTAMP NULL
);

-- down
DROP TABLE products;