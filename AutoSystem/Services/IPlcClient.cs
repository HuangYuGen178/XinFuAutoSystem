using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem.Services
{
    // PLC 抽象接口，统一上层调用
    public interface IPlcClient
    {
        Task<int> ReadintAsync(string address);
        Task WriteintAsync(string address, int value);
        Task<string> ReadStringAsync(string address, ushort length);
        Task WriteStringAsync(string address, string value);
        Task<bool> IsConnectedAsync();
    }
}
