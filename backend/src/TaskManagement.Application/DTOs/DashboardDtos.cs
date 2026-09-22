namespace TaskManagement.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalCount { get; set; }
    public int ToDoCount { get; set; }
    public int InProgressCount { get; set; }
    public int DoneCount { get; set; }
    public int OverdueCount { get; set; }
    public int HighPriorityCount { get; set; }
    public List<TaskDto> UpcomingDeadlines { get; set; } = new();
}
