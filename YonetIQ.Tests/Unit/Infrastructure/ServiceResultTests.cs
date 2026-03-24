using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Tests.Unit.Infrastructure;

public class ServiceResultTests
{
    [Fact]
    public void Success_Generic_SetsIsSuccessTrue()
    {
        var result = ServiceResult<int>.Success(42, "ok");
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
        Assert.Equal("ok", result.Message);
    }

    [Fact]
    public void Success_Generic_DefaultMessage_IsEmpty()
    {
        var result = ServiceResult<string>.Success("data");
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Message);
    }

    [Fact]
    public void Failure_Generic_SetsIsSuccessFalse()
    {
        var result = ServiceResult<int>.Failure("hata", "ERR001");
        Assert.False(result.IsSuccess);
        Assert.Equal("hata", result.Message);
        Assert.Equal("ERR001", result.ErrorCode);
    }

    [Fact]
    public void Failure_Generic_ErrorCodeIsOptional()
    {
        var result = ServiceResult<int>.Failure("hata");
        Assert.False(result.IsSuccess);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void NonGeneric_Success_Works()
    {
        var result = ServiceResult.Success("tamam");
        Assert.True(result.IsSuccess);
        Assert.Equal("tamam", result.Message);
    }

    [Fact]
    public void NonGeneric_Failure_Works()
    {
        var result = ServiceResult.Failure("hata", "ERR");
        Assert.False(result.IsSuccess);
        Assert.Equal("hata", result.Message);
        Assert.Equal("ERR", result.ErrorCode);
    }
}
