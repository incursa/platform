DO $$
DECLARE
    schema_name text := '$SchemaName$';
    series_table regclass;
    minute_table regclass;
    hourly_table regclass;
BEGIN
    series_table := to_regclass(format('%I.%I', schema_name, 'MetricSeries'));
    IF series_table IS NULL THEN
        RETURN;
    END IF;

    EXECUTE format(
        'ALTER TABLE %I.%I
            ADD COLUMN IF NOT EXISTS "DatabaseId" uuid NOT NULL DEFAULT ''00000000-0000-0000-0000-000000000000''',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'ALTER TABLE %I.%I
            ALTER COLUMN "InstanceId" SET DEFAULT ''00000000-0000-0000-0000-000000000000''',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'ALTER TABLE %I.%I
            ALTER COLUMN "DatabaseId" SET DEFAULT ''00000000-0000-0000-0000-000000000000''',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'UPDATE %I.%I
            SET "InstanceId" = ''00000000-0000-0000-0000-000000000000''
          WHERE "InstanceId" IS NULL',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'UPDATE %I.%I
            SET "DatabaseId" = COALESCE(NULLIF("InstanceId", ''00000000-0000-0000-0000-000000000000''), ''00000000-0000-0000-0000-000000000000'')
          WHERE "DatabaseId" IS NULL
             OR "DatabaseId" = ''00000000-0000-0000-0000-000000000000''',
        schema_name,
        'MetricSeries');

    CREATE TEMP TABLE _metric_series_map (
        old_series_id bigint PRIMARY KEY,
        new_series_id bigint NOT NULL
    ) ON COMMIT DROP;

    EXECUTE format(
        'INSERT INTO _metric_series_map (old_series_id, new_series_id)
         SELECT s."SeriesId" AS old_series_id,
                MIN(s."SeriesId") OVER (
                    PARTITION BY s."MetricDefId", s."DatabaseId", s."Service", s."TagHash"
                ) AS new_series_id
           FROM %I.%I s',
        schema_name,
        'MetricSeries');

    DELETE FROM _metric_series_map WHERE old_series_id = new_series_id;

    minute_table := to_regclass(format('%I.%I', schema_name, 'MetricPointMinute'));
    IF minute_table IS NOT NULL THEN
        EXECUTE format(
            'INSERT INTO %I.%I AS mp (
                "SeriesId", "BucketStartUtc", "BucketSecs",
                "ValueSum", "ValueCount", "ValueMin", "ValueMax", "ValueLast", "P50", "P95", "P99"
            )
            SELECT
                m.new_series_id,
                p."BucketStartUtc",
                p."BucketSecs",
                SUM(p."ValueSum"),
                SUM(p."ValueCount"),
                MIN(p."ValueMin"),
                MAX(p."ValueMax"),
                (ARRAY_AGG(p."ValueLast" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P50" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P95" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P99" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1]
            FROM %I.%I p
            JOIN _metric_series_map m ON m.old_series_id = p."SeriesId"
            GROUP BY m.new_series_id, p."BucketStartUtc", p."BucketSecs"
            ON CONFLICT ("SeriesId", "BucketStartUtc", "BucketSecs") DO UPDATE
            SET "ValueSum" = COALESCE(mp."ValueSum", 0) + COALESCE(EXCLUDED."ValueSum", 0),
                "ValueCount" = COALESCE(mp."ValueCount", 0) + COALESCE(EXCLUDED."ValueCount", 0),
                "ValueMin" = CASE
                    WHEN mp."ValueMin" IS NULL THEN EXCLUDED."ValueMin"
                    WHEN EXCLUDED."ValueMin" IS NULL THEN mp."ValueMin"
                    WHEN EXCLUDED."ValueMin" < mp."ValueMin" THEN EXCLUDED."ValueMin"
                    ELSE mp."ValueMin"
                END,
                "ValueMax" = CASE
                    WHEN mp."ValueMax" IS NULL THEN EXCLUDED."ValueMax"
                    WHEN EXCLUDED."ValueMax" IS NULL THEN mp."ValueMax"
                    WHEN EXCLUDED."ValueMax" > mp."ValueMax" THEN EXCLUDED."ValueMax"
                    ELSE mp."ValueMax"
                END,
                "ValueLast" = EXCLUDED."ValueLast",
                "InsertedUtc" = CURRENT_TIMESTAMP',
            schema_name,
            'MetricPointMinute',
            schema_name,
            'MetricPointMinute');

        EXECUTE format(
            'DELETE FROM %I.%I p
             USING _metric_series_map m
             WHERE p."SeriesId" = m.old_series_id',
            schema_name,
            'MetricPointMinute');
    END IF;

    hourly_table := to_regclass(format('%I.%I', schema_name, 'MetricPointHourly'));
    IF hourly_table IS NOT NULL THEN
        EXECUTE format(
            'INSERT INTO %I.%I AS mp (
                "SeriesId", "BucketStartUtc", "BucketSecs",
                "ValueSum", "ValueCount", "ValueMin", "ValueMax", "ValueLast", "P50", "P95", "P99"
            )
            SELECT
                m.new_series_id,
                p."BucketStartUtc",
                p."BucketSecs",
                SUM(p."ValueSum"),
                SUM(p."ValueCount"),
                MIN(p."ValueMin"),
                MAX(p."ValueMax"),
                (ARRAY_AGG(p."ValueLast" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P50" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P95" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1],
                (ARRAY_AGG(p."P99" ORDER BY p."InsertedUtc" DESC NULLS LAST))[1]
            FROM %I.%I p
            JOIN _metric_series_map m ON m.old_series_id = p."SeriesId"
            GROUP BY m.new_series_id, p."BucketStartUtc", p."BucketSecs"
            ON CONFLICT ("SeriesId", "BucketStartUtc", "BucketSecs") DO UPDATE
            SET "ValueSum" = COALESCE(mp."ValueSum", 0) + COALESCE(EXCLUDED."ValueSum", 0),
                "ValueCount" = COALESCE(mp."ValueCount", 0) + COALESCE(EXCLUDED."ValueCount", 0),
                "ValueMin" = CASE
                    WHEN mp."ValueMin" IS NULL THEN EXCLUDED."ValueMin"
                    WHEN EXCLUDED."ValueMin" IS NULL THEN mp."ValueMin"
                    WHEN EXCLUDED."ValueMin" < mp."ValueMin" THEN EXCLUDED."ValueMin"
                    ELSE mp."ValueMin"
                END,
                "ValueMax" = CASE
                    WHEN mp."ValueMax" IS NULL THEN EXCLUDED."ValueMax"
                    WHEN EXCLUDED."ValueMax" IS NULL THEN mp."ValueMax"
                    WHEN EXCLUDED."ValueMax" > mp."ValueMax" THEN EXCLUDED."ValueMax"
                    ELSE mp."ValueMax"
                END,
                "ValueLast" = EXCLUDED."ValueLast",
                "InsertedUtc" = CURRENT_TIMESTAMP',
            schema_name,
            'MetricPointHourly',
            schema_name,
            'MetricPointHourly');

        EXECUTE format(
            'DELETE FROM %I.%I p
             USING _metric_series_map m
             WHERE p."SeriesId" = m.old_series_id',
            schema_name,
            'MetricPointHourly');
    END IF;

    EXECUTE format(
        'DELETE FROM %I.%I s
         USING _metric_series_map m
         WHERE s."SeriesId" = m.old_series_id',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'ALTER TABLE %I.%I DROP CONSTRAINT IF EXISTS "UQ_MetricSeries"',
        schema_name,
        'MetricSeries');

    EXECUTE format(
        'DROP INDEX IF EXISTS %I.%I',
        schema_name,
        'UQ_MetricSeries_Def_Service_Instance_TagHash');

    EXECUTE format(
        'CREATE UNIQUE INDEX IF NOT EXISTS "UQ_MetricSeries_Def_Db_Service_TagHash"
            ON %I.%I ("MetricDefId", "DatabaseId", "Service", "TagHash")',
        schema_name,
        'MetricSeries');
END $$;
