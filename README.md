# CourseWiki
Core service of CourseWiki
## How to start developing on this Project?
First you need to install [.Net Core](https://dotnet.microsoft.com/download) and [PostgreSQL](https://www.postgresql.org/download/). To Set up PostgreSQL, [this link](https://wiki.archlinux.org/title/PostgreSQL) maybe helpful, for this project we need the database name username/password with enought permission on the database.
Before run this project you need to fill information in `CourseWiki/appsettings.json`.


Listed below are all the possible options that can be entered in your `appsettings.json` file.

## Basics

The configuration settings in the basics section are **required** and must be defined in your  `appsettings.json` file.


### Database

#### PostgreSQL

> **PostgreSQL** is the recommended engine for best performance, features and future compatibility.

```
"ConnectionStrings": {
    "rmfDatabase": "Host=127.0.0.1;Username=coursewiki;Password=coursewiki;Database=coursewiki"
  }
```

### Jwt Secret

You need generate a random string which have 16 characters, for example:

```
"Secret": "XIzMRZDGqljQjpTr"
```

### Email settings

You need setting up email so account system can run normally, we suggest use fake email service like ethereal.email to debuging on email problem, for example:

```
"EmailFrom": "haha@ethereal.email",
"SmtpHost": "smtp.ethereal.email",
"SmtpPort": 587,
"SmtpUser": "haha@ethereal.email",
"SmtpPass": "wTaYqHgObfErgdEK"
```
**Notes**:
- this is a example, please user your own email credential from your email service like ethereal.email.

## Bootstrap Database
First you need to install EF Core tools:
`dotnet tool install --global dotnet-ef`
Follow the instructions to set up `PATHS`, then you need to run:
`dotnet-ef migrations add "Add_new_tables"` and
`dotnet-ef database update` in the CourseWiki directory inside the repo.

Now the database is setting up, if it have any error output, please maske sure database location and crenditial are provided correctly.

## Run the project
Finally you can run this project, Just run `dotnet run` in CourseWiki directory inside the repo. the default server will start on `http://localhost:5000`, for API documentation you can see it in `http://localhost:5000/swagger/index.html`.

## import course information from UoA api

To import the course information, you can just use this as example to import COMPSCI course from 2018 to 2021 and MATHS course from 2017 to 2021.

```
curl -X 'POST' \
  'http://localhost:5000/api/Init/courseInit' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
  "initSubjects": [
    {
      "subject": "COMPSCI",
      "startYear": 2018,
      "endYear": 2021
    },
    {
      "subject": "MATHS",
      "startYear": 2017,
      "endYear": 2021
    }
  ]
}'
```
