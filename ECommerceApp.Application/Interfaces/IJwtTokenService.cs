namespace ECommerceApp.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string Create(string subject, string roleName, int roleId);
    }
}
