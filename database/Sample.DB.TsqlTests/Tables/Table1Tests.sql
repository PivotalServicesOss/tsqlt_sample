EXEC tSQLt.NewTestClass 'Table1Tests';
GO

CREATE PROCEDURE Table1Tests.[test if Table1 table is composed of]
AS
BEGIN
	-- Arrange
    CREATE TABLE Expected
    (
        COLUMN_NAME VARCHAR(20), 
		COLUMN_DEFAULT VARCHAR(20), 
		IS_NULLABLE VARCHAR(20), 
		DATA_TYPE VARCHAR(20), 
		CHARACTER_MAXIMUM_LENGTH VARCHAR(20)
    )

	SELECT * INTO #Actual FROM Expected 

	INSERT INTO Expected VALUES ('Id',null,'NO','int',null)
	INSERT INTO Expected VALUES ('Text',null,'NO','varchar','50')
	INSERT INTO Expected VALUES ('Description',null,'YES','varchar','500')

	-- Act
	INSERT INTO #Actual
    SELECT COLUMN_NAME, 
			COLUMN_DEFAULT, 
			IS_NULLABLE, 
			DATA_TYPE, 
			CHARACTER_MAXIMUM_LENGTH AS MAX_LENGTH 
	FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Table1'

	--Assert
    EXEC tSQLt.AssertEqualsTable 'Expected', '#Actual';
END;
GO