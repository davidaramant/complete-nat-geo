# complete-nat-geo
A local web app for browsing The Complete National Geographic

## Merge legacy databases

Create a new SQLite database from the 2009 base database and each later content database:

```powershell
dotnet run --project src/PostgresBuilder -- merge `
	--sqlite-path C:\path\to\cngcontent2009.sqlite3 `
	--source-sqlite-path C:\path\to\content2010.sqlite3 `
	--source-sqlite-path C:\path\to\content2011.sqlite3 `
	--source-sqlite-path C:\path\to\content2012.sqlite3 `
	--source-sqlite-path C:\path\to\content2013.sqlite3 `
	--output-sqlite-path C:\path\to\complete.sqlite3
```

The action never changes an input database. It rejects duplicate issue `search_time` values, incompatible schemas, invalid relationships, trivia rows in more than one input database, and an existing output path.
