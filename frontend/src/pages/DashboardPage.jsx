import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getDashboardSummary } from "../api/dashboard";
import { extractErrorMessage } from "../api/client";
import StatusBadge from "../components/StatusBadge";
import PriorityBadge from "../components/PriorityBadge";
import { IconAlert, IconCalendar, IconCheckSquare, IconGrid } from "../components/icons";

export default function DashboardPage() {
  const [summary, setSummary] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getDashboardSummary()
      .then(setSummary)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  }, []);

  if (loading)
    return (
      <div className="page-loading">
        <span className="spinner" />
        <span>Loading dashboard...</span>
      </div>
    );
  if (error) return <div className="alert alert-error">{error}</div>;
  if (!summary) return null;

  const cards = [
    { label: "Total tasks", value: summary.totalCount, tone: "neutral", icon: IconGrid },
    { label: "To Do", value: summary.toDoCount, tone: "todo", icon: IconCheckSquare },
    { label: "In Progress", value: summary.inProgressCount, tone: "progress", icon: IconCheckSquare },
    { label: "Done", value: summary.doneCount, tone: "done", icon: IconCheckSquare },
    { label: "Overdue", value: summary.overdueCount, tone: "overdue", icon: IconAlert },
    { label: "High priority open", value: summary.highPriorityCount, tone: "high", icon: IconAlert },
  ];

  return (
    <div>
      <h1 className="page-title">Dashboard</h1>

      <div className="stat-grid">
        {cards.map((card) => {
          const Icon = card.icon;
          return (
            <div className={`stat-card tone-${card.tone}`} key={card.label}>
              <span className="stat-icon">
                <Icon width={17} height={17} />
              </span>
              <span className="stat-value">{card.value}</span>
              <span className="stat-label">{card.label}</span>
            </div>
          );
        })}
      </div>

      <section className="panel">
        <h2>
          <IconCalendar width={16} height={16} style={{ verticalAlign: -3, marginRight: 6 }} />
          Upcoming deadlines
        </h2>
        {summary.upcomingDeadlines.length === 0 ? (
          <p className="empty-state">Nothing due soon.</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>Due</th>
                  <th>Assignee</th>
                </tr>
              </thead>
              <tbody>
                {summary.upcomingDeadlines.map((task) => (
                  <tr key={task.id}>
                    <td>
                      <Link to={`/tasks/${task.id}`}>{task.title}</Link>
                    </td>
                    <td>
                      <StatusBadge status={task.status} />
                    </td>
                    <td>
                      <PriorityBadge priority={task.priority} />
                    </td>
                    <td>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : "—"}</td>
                    <td>{task.assignedToName || "Unassigned"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
