// Bản save gửi lên / tải về từ cloud (UserDto gốc) — tên field giữ nguyên như server.
public class UserDto
{
    public string UserJson;             // toàn bộ UserData dạng json
    public int SaveVersion;
    public long LastSyncUtcMs;          // giờ server ghi nhận lần lưu
    public int SchemaVersion;
    public string LastWriterDeviceId;   // máy đã ghi bản này
}

public class SaveUserDocResponse
{
    public bool success;
    public string message;
    public int SaveVersion;
    public long LastSyncUtcMs;
    public string LastWriterDeviceId;
}

public class LoadUserDocResponse : UserDto
{
    public bool success;
    public string message;
    public bool exists;
}

public class AccountResponse
{
    public bool success;
    public string message;
}
