using System;

namespace FluentGit.Services;

// 提取为顶级类，方便 XAML 的 x:DataType 解析
public class GitStatusEntry
{
    public string? Path { get; set; }
    public char X { get; set; }   // 暂存区状态
    public char Y { get; set; }   // 工作区状态
    
    // 供 UI 直接绑定的状态文本
    public string StatusText 
    {
        get
        {
            if (X == '?' || Y == '?') return "未跟踪";
            if (X == 'A') return "已暂存";
            if (Y == 'M' || X == 'M') return "已修改";
            if (Y == 'D' || X == 'D') return "已删除";
            return "已变更";
        }
    }
}

public static partial class GitService
{
    // 里面空了，或者你有别的模型就放这里
}