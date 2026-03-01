IF OBJECT_ID(N'[$SchemaName$].MetricSeries', N'U') IS NULL
    RETURN;
GO

IF COL_LENGTH('[$SchemaName$].MetricSeries', 'DatabaseId') IS NULL
BEGIN
    ALTER TABLE [$SchemaName$].MetricSeries
        ADD DatabaseId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_MetricSeries_DatabaseId DEFAULT ('00000000-0000-0000-0000-000000000000');
END
GO

UPDATE [$SchemaName$].MetricSeries
SET InstanceId = '00000000-0000-0000-0000-000000000000'
WHERE InstanceId IS NULL;
GO

UPDATE [$SchemaName$].MetricSeries
SET DatabaseId = CASE
    WHEN DatabaseId IS NULL OR DatabaseId = '00000000-0000-0000-0000-000000000000' THEN
        ISNULL(NULLIF(InstanceId, '00000000-0000-0000-0000-000000000000'), '00000000-0000-0000-0000-000000000000')
    ELSE DatabaseId
END;
GO

IF OBJECT_ID(N'tempdb..#MetricSeriesMap', N'U') IS NOT NULL
    DROP TABLE #MetricSeriesMap;

SELECT
    old_series_id = s.SeriesId,
    new_series_id = MIN(s.SeriesId) OVER (
        PARTITION BY s.MetricDefId, s.DatabaseId, s.Service, s.TagHash
    )
INTO #MetricSeriesMap
FROM [$SchemaName$].MetricSeries s;

DELETE FROM #MetricSeriesMap WHERE old_series_id = new_series_id;
GO

IF OBJECT_ID(N'[$SchemaName$].MetricPointMinute', N'U') IS NOT NULL
BEGIN
    ;WITH src AS (
        SELECT
            m.new_series_id AS SeriesId,
            p.BucketStartUtc,
            p.BucketSecs,
            ValueSum = SUM(ISNULL(p.ValueSum, 0.0)),
            ValueCount = SUM(ISNULL(p.ValueCount, 0)),
            ValueMin = MIN(p.ValueMin),
            ValueMax = MAX(p.ValueMax)
        FROM [$SchemaName$].MetricPointMinute p
        INNER JOIN #MetricSeriesMap m ON m.old_series_id = p.SeriesId
        GROUP BY m.new_series_id, p.BucketStartUtc, p.BucketSecs
    )
    MERGE [$SchemaName$].MetricPointMinute AS tgt
    USING src
      ON tgt.SeriesId = src.SeriesId
     AND tgt.BucketStartUtc = src.BucketStartUtc
     AND tgt.BucketSecs = src.BucketSecs
    WHEN MATCHED THEN
      UPDATE SET
        ValueSum = ISNULL(tgt.ValueSum, 0.0) + src.ValueSum,
        ValueCount = ISNULL(tgt.ValueCount, 0) + src.ValueCount,
        ValueMin = CASE WHEN tgt.ValueMin IS NULL OR src.ValueMin < tgt.ValueMin THEN src.ValueMin ELSE tgt.ValueMin END,
        ValueMax = CASE WHEN tgt.ValueMax IS NULL OR src.ValueMax > tgt.ValueMax THEN src.ValueMax ELSE tgt.ValueMax END,
        InsertedUtc = SYSDATETIMEOFFSET()
    WHEN NOT MATCHED THEN
      INSERT (SeriesId, BucketStartUtc, BucketSecs, ValueSum, ValueCount, ValueMin, ValueMax)
      VALUES (src.SeriesId, src.BucketStartUtc, src.BucketSecs, src.ValueSum, src.ValueCount, src.ValueMin, src.ValueMax);

    DELETE p
    FROM [$SchemaName$].MetricPointMinute p
    INNER JOIN #MetricSeriesMap m ON m.old_series_id = p.SeriesId;
END
GO

IF OBJECT_ID(N'[$SchemaName$].MetricPointHourly', N'U') IS NOT NULL
BEGIN
    ;WITH src AS (
        SELECT
            m.new_series_id AS SeriesId,
            p.BucketStartUtc,
            p.BucketSecs,
            ValueSum = SUM(ISNULL(p.ValueSum, 0.0)),
            ValueCount = SUM(ISNULL(p.ValueCount, 0)),
            ValueMin = MIN(p.ValueMin),
            ValueMax = MAX(p.ValueMax)
        FROM [$SchemaName$].MetricPointHourly p
        INNER JOIN #MetricSeriesMap m ON m.old_series_id = p.SeriesId
        GROUP BY m.new_series_id, p.BucketStartUtc, p.BucketSecs
    )
    MERGE [$SchemaName$].MetricPointHourly AS tgt
    USING src
      ON tgt.SeriesId = src.SeriesId
     AND tgt.BucketStartUtc = src.BucketStartUtc
     AND tgt.BucketSecs = src.BucketSecs
    WHEN MATCHED THEN
      UPDATE SET
        ValueSum = ISNULL(tgt.ValueSum, 0.0) + src.ValueSum,
        ValueCount = ISNULL(tgt.ValueCount, 0) + src.ValueCount,
        ValueMin = CASE WHEN tgt.ValueMin IS NULL OR src.ValueMin < tgt.ValueMin THEN src.ValueMin ELSE tgt.ValueMin END,
        ValueMax = CASE WHEN tgt.ValueMax IS NULL OR src.ValueMax > tgt.ValueMax THEN src.ValueMax ELSE tgt.ValueMax END,
        InsertedUtc = SYSDATETIMEOFFSET()
    WHEN NOT MATCHED THEN
      INSERT (SeriesId, BucketStartUtc, BucketSecs, ValueSum, ValueCount, ValueMin, ValueMax)
      VALUES (src.SeriesId, src.BucketStartUtc, src.BucketSecs, src.ValueSum, src.ValueCount, src.ValueMin, src.ValueMax);

    DELETE p
    FROM [$SchemaName$].MetricPointHourly p
    INNER JOIN #MetricSeriesMap m ON m.old_series_id = p.SeriesId;
END
GO

DELETE s
FROM [$SchemaName$].MetricSeries s
INNER JOIN #MetricSeriesMap m ON m.old_series_id = s.SeriesId;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_MetricSeries_Def_Service_Instance_TagHash'
      AND object_id = OBJECT_ID(N'[$SchemaName$].MetricSeries', N'U'))
BEGIN
    DROP INDEX UQ_MetricSeries_Def_Service_Instance_TagHash ON [$SchemaName$].MetricSeries;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [type] = 'UQ'
      AND [name] = 'UQ_MetricSeries'
      AND [parent_object_id] = OBJECT_ID(N'[$SchemaName$].MetricSeries', N'U'))
BEGIN
    ALTER TABLE [$SchemaName$].MetricSeries DROP CONSTRAINT UQ_MetricSeries;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_MetricSeries_Def_Db_Service_TagHash'
      AND object_id = OBJECT_ID(N'[$SchemaName$].MetricSeries', N'U'))
BEGIN
    CREATE UNIQUE INDEX UQ_MetricSeries_Def_Db_Service_TagHash
        ON [$SchemaName$].MetricSeries (MetricDefId, DatabaseId, Service, TagHash);
END
GO
