import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import * as tasksApi from "../api/tasks";
import { getUsers } from "../api/users";
import { getTeams } from "../api/teams";
import { extractErrorMessage } from "../api/client";
import StatusBadge from "../components/StatusBadge";
import PriorityBadge from "../components/PriorityBadge";
import TaskFormModal from "../components/TaskFormModal";
import ConfirmDialog from "../components/ConfirmDialog";
import { IconPlus, IconSearch } from "../components/icons";

const STATUSES = ["ToDo", "InProgress", "Done"];
const PRIORITIES = ["Low", "Medium", "High"];

export default function TasksPage() {
  const { user } = useAuth();
  const canManage = user.role === "Admin" || user.role === "Manager";

  const [tasks, setTasks] = useState([]);
  const [users, setUsers] = useState([]);
  const [teams, setTeams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [filters, setFilters] = useState({ status: "", priority: "", dueBefore: "", search: "" });
  const [showForm, setShowForm] = useState(false);
  const [editingTask, setEditingTask] = useState(null);
  const [deletingTask, setDeletingTask] = useState(null);

  const loadTasks = useCallback(() => {
    setLoading(true);
    const params = {};
    if (filters.status) params.status = filters.status;
    if (filters.priority) params.priority = filters.priority;
    if (filters.dueBefore) params.dueBefore = new Date(filters.dueBefore).toISOString();
    if (filters.search) params.search = filters.search;

    tasksApi
      .getTasks(params)
      .then(setTasks)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [filters]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  useEffect(() => {
    if (canManage) {
      getUsers().then(setUsers).catch(() => {});
      getTeams().then(setTeams).catch(() => {});
    }
  }, [canManage]);

  const handleStatusChange = async (task, status) => {
    try {
      await tasksApi.updateTaskStatus(task.id, status);
      loadTasks();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleCreate = async (payload) => {
    await tasksApi.createTask(payload);
    loadTasks();
  };

  const handleUpdate = async (payload) => {
    await tasksApi.updateTask(editingTask.id, payload);
    loadTasks();
  };

  const handleDelete = async () => {
    try {
      await tasksApi.deleteTask(deletingTask.id);
      setDeletingTask(null);
      loadTasks();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  return (
    <div>
      <div className="page-header-row">
        <h1 className="page-title">Tasks</h1>
        {canManage && (
          <button className="btn btn-primary" onClick={() => setShowForm(true)}>
            <IconPlus width={16} height={16} />
            New task
          </button>
        )}
      </div>

      <div className="filter-bar">
        <div className="input-with-icon" style={{ flex: 1, minWidth: 220 }}>
          <IconSearch width={16} height={16} />
          <input
            type="search"
            placeholder="Search title or description..."
            value={filters.search}
            onChange={(e) => setFilters((f) => ({ ...f, search: e.target.value }))}
          />
        </div>
        <select
          value={filters.status}
          onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value }))}
        >
          <option value="">All statuses</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <select
          value={filters.priority}
          onChange={(e) => setFilters((f) => ({ ...f, priority: e.target.value }))}
        >
          <option value="">All priorities</option>
          {PRIORITIES.map((p) => (
            <option key={p} value={p}>
              {p}
            </option>
          ))}
        </select>
        <label className="filter-date-label">
          Due before
          <input
            type="date"
            value={filters.dueBefore}
            onChange={(e) => setFilters((f) => ({ ...f, dueBefore: e.target.value }))}
          />
        </label>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <div className="page-loading">
          <span className="spinner" />
          <span>Loading tasks...</span>
        </div>
      ) : tasks.length === 0 ? (
        <p className="empty-state">No tasks match your filters.</p>
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
              <th>Team</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {tasks.map((task) => {
              const canUpdateStatus = canManage || task.assignedToId === user.id;
              return (
                <tr key={task.id}>
                  <td>
                    <Link to={`/tasks/${task.id}`}>{task.title}</Link>
                    {task.commentCount > 0 && <span className="comment-count"> ({task.commentCount})</span>}
                  </td>
                  <td>
                    {canUpdateStatus ? (
                      <select
                        value={task.status}
                        onChange={(e) => handleStatusChange(task, e.target.value)}
                        className={`status-select status-${task.status}`}
                      >
                        {STATUSES.map((s) => (
                          <option key={s} value={s}>
                            {s}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <StatusBadge status={task.status} />
                    )}
                  </td>
                  <td>
                    <PriorityBadge priority={task.priority} />
                  </td>
                  <td className={isOverdue(task) ? "overdue-text" : ""}>
                    {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : "—"}
                  </td>
                  <td>{task.assignedToName || "Unassigned"}</td>
                  <td>{task.teamName || "—"}</td>
                  <td className="row-actions">
                    {canManage && (
                      <>
                        <button className="link-button" onClick={() => setEditingTask(task)}>
                          Edit
                        </button>
                        <button className="link-button danger" onClick={() => setDeletingTask(task)}>
                          Delete
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        </div>
      )}

      {showForm && (
        <TaskFormModal
          users={users}
          teams={teams}
          defaultTeamId={teams[0]?.id}
          onClose={() => setShowForm(false)}
          onSubmit={handleCreate}
        />
      )}

      {editingTask && (
        <TaskFormModal
          task={editingTask}
          users={users}
          teams={teams}
          onClose={() => setEditingTask(null)}
          onSubmit={handleUpdate}
        />
      )}

      {deletingTask && (
        <ConfirmDialog
          title="Delete task"
          message={`Delete "${deletingTask.title}"? This cannot be undone.`}
          confirmLabel="Delete"
          onCancel={() => setDeletingTask(null)}
          onConfirm={handleDelete}
        />
      )}
    </div>
  );
}

function isOverdue(task) {
  return task.dueDate && new Date(task.dueDate) < new Date() && task.status !== "Done";
}
