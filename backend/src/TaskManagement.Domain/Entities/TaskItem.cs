using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
