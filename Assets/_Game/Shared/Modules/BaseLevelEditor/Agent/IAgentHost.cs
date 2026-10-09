namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Phần riêng của từng game cho trợ lý: nó được dặn gì, biết gì về level đang mở và có những câu gợi ý nào.</summary>
    public interface IAgentHost
    {
        string SystemPrompt { get; }

        string[] QuickPrompts { get; }

        /// <summary>Danh sách kiểu đề xuất (kind) và các trường kèm theo, ghép vào lời dặn.</summary>
        string ProposalGuide { get; }

        /// <summary>Tóm tắt level đang mở, đính kèm đầu mỗi tin nhắn (gọi ở luồng chính).</summary>
        string BuildContext();

        /// <summary>Chỉ ra chỗ đề xuất trên tranh hoặc hàng chờ (chọn mảnh, hộp liên quan).</summary>
        void Preview(AgentProposal proposal);

        /// <summary>Kiểm tra đề xuất có hợp lệ với level hiện tại không (id còn tồn tại, mảnh kề nhau...); null là hợp lệ, không thì trả lý do; note là hệ quả đáng chú ý nếu áp dụng (vd cát lệch), có thể null.</summary>
        string Validate(AgentProposal proposal, out string note);

        /// <summary>Áp dụng một đề xuất vào level qua hệ thống undo; sai thì trả false kèm lý do.</summary>
        bool Apply(AgentProposal proposal, out string error);
    }
}
