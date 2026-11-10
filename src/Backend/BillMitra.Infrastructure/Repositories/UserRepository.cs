using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, username AS Username, full_name AS FullName, email AS Email,
                   password_hash AS PasswordHash, is_active AS IsActive, created_at_utc AS CreatedAtUtc
            FROM users
            WHERE LOWER(username) = LOWER(@Username)";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<bool> HasUsersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM users";
        var count = await connection.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<User> CreateAsync(User user, string[] roleNames, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string insertUserSql = @"
            INSERT INTO users (id, username, full_name, email, password_hash, is_active, created_at_utc)
            VALUES (@Id, @Username, @FullName, @Email, @PasswordHash, @IsActive, @CreatedAtUtc)";

        await connection.ExecuteAsync(insertUserSql, user);

        foreach (var roleName in roleNames)
        {
            var roleId = Guid.NewGuid();
            // Ensure role exists
            const string ensureRoleSql = @"
                INSERT INTO roles (id, name, description)
                SELECT @RoleId, @RoleName, @Description
                WHERE NOT EXISTS (SELECT 1 FROM roles WHERE LOWER(name) = LOWER(@RoleName))";

            await connection.ExecuteAsync(ensureRoleSql, new { RoleId = roleId, RoleName = roleName, Description = $"{roleName} Role" });

            const string getRoleIdSql = "SELECT id FROM roles WHERE LOWER(name) = LOWER(@RoleName)";
            var actualRoleId = await connection.ExecuteScalarAsync<Guid>(getRoleIdSql, new { RoleName = roleName });

            const string linkUserRoleSql = @"
                INSERT INTO user_roles (user_id, role_id)
                VALUES (@UserId, @RoleId)";

            await connection.ExecuteAsync(linkUserRoleSql, new { UserId = user.Id, RoleId = actualRoleId });
        }

        return user;
    }
}

