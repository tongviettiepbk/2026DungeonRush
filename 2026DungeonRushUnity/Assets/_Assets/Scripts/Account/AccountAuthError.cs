// Lỗi đăng nhập / đăng ký — lấy đúng tên + giá trị của Firebase.Auth.AuthError mà AccountController gốc phân nhánh
// để chọn thông báo (cnd / cne / cnf).
public enum AccountAuthError
{
    None = 0,
    Failure = 1,
    UserDisabled = 5,
    EmailAlreadyInUse = 8,
    InvalidEmail = 11,
    WrongPassword = 12,
    TooManyRequests = 13,
    UserNotFound = 14,
    ProviderAlreadyLinked = 15,
    NetworkRequestFailed = 19,
    WeakPassword = 23,
}
