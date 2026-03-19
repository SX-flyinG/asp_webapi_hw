using asp_webapi_hw.DTO;
using asp_webapi_hw.Models;

namespace asp_webapi_hw.Services;

public interface ITokenService
{
    TokenResponseDto GenerateTokenPair(User user);

    TokenResponseDto? RefreshTokenPair(string refreshToken);

    bool RevokeToken(string refreshToken);

    void RevokeAllUserTokens(int userId);

    int? ValidateAccessToken(string accessToken);
}
