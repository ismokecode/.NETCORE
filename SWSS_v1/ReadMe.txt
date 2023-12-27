If token is expired so it should be generate refresh token.

GenerateToken - JwtToken (change)+ Refresh Token(won't change)
JwtTokenExpired - ExpireToken + RefreshToken = Generate New Refresh Token

Microsoft.EntityFrameworkCore.SqlServer;
Microsoft.EntityFrameworkCore.Tools;
Microsoft.AspNetCore.Identity.EntityFrameworkCore;
Microsoft.AspNetCore.Authentication.JwtBearer;

//Run command#1 Add-Migration IdentityTablesAdded generate identity table using code first approch 
//Error then Install Microsoft.EntityFrameworkCore.Tools (use latest versions)

//If needed Run command#1 add-migration InitialMigration
//Run command#2 update-database (Code first approch db has been created)

----Request pipeling middleware
Request > ExceptionHandler > HSTS > HTTPRedirection > StaticFiles > Routing > CORS > 
Authantication > Authorization > Custom Middleware 1 > Custom Middleware 2 > End Points
> Generate Response

