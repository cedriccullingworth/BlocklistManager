----INSERT INTO CUSTOMERS_TABLE (DOCUMENT, NAME, ADDRESS, PROFESSION)
--SELECT
--   MY_XML.shodan.query('ipv4').value('.', 'VARCHAR(20)'),
--   MY_XML.Customer.query('date').value('.', 'VARCHAR(50)'),
--   MY_XML.Customer.query('lastseen').value('.', 'VARCHAR(50)')
select *
FROM (SELECT CAST(Shodan AS xml)
      FROM OPENROWSET(BULK 'D:\Projects\BlocklistManager\SampleFiles\Shodan.xml', SINGLE_BLOB) AS Shodan T(MY_XML)) AS T(MY_XML)
      CROSS APPLY MY_XML.nodes('threatlist/shodan') AS MY_XML.query(shodan)

drop table #XML;

SELECT CAST(MY_XML as xml) AS rawData
--INTO #XML
      FROM OPENROWSET(BULK 'D:\Projects\BlocklistManager\SampleFiles\Shodan.xml', SINGLE_BLOB) AS T(MY_XML)

--select *
--(
	SELECT CAST( rawData AS xml ) AS MY_XML FROM #XML
--) x-- MY_XML
CROSS APPLY MY_XML.nodes('threatlist/shodan') AS MY_XML.query()

