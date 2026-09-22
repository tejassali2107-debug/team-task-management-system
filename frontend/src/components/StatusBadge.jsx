const LABELS = {
  ToDo: "To Do",
  InProgress: "In Progress",
  Done: "Done",
};

export default function StatusBadge({ status }) {
  return <span className={`badge status-${status}`}>{LABELS[status] || status}</span>;
}
