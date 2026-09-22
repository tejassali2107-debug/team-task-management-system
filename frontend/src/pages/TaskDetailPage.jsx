import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import * as tasksApi from "../api/tasks";
import { extractErrorMessage } from "../api/client";
import StatusBadge from "../components/StatusBadge";
import PriorityBadge from "../components/PriorityBadge";
import { IconArrowLeft } from "../components/icons";

const STATUSES = ["ToDo", "InProgress", "Done"];

export default function TaskDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [task, setTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [newComment, setNewComment] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [posting, setPosting] = useState(false);

  const canManage = user.role === "Admin" || user.role === "Manager";

  const load = () => {
    setLoading(true);
    Promise.all([tasksApi.getTask(id), tasksApi.getComments(id)])
      .then(([taskData, commentData]) => {
        setTask(taskData);
        setComments(commentData);
      })
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleStatusChange = async (status) => {
    try {
      const updated = await tasksApi.updateTaskStatus(id, status);
      setTask(updated);
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleAddComment = async (e) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    setPosting(true);
    try {
      const comment = await tasksApi.addComment(id, newComment.trim());
      setComments((c) => [...c, comment]);
      setNewComment("");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setPosting(false);
    }
  };

  if (loading)
    return (
      <div className="page-loading">
        <span className="spinner" />
        <span>Loading task...</span>
      </div>
    );
  if (error && !task) return <div className="alert alert-error">{error}</div>;
  if (!task) return null;

  const canUpdateStatus = canManage || task.assignedToId === user.id;

  return (
    <div>
      <button className="link-button back-link" onClick={() => navigate(-1)}>
        <IconArrowLeft width={15} height={15} />
        Back
      </button>

      <div className="task-detail-header">
        <h1 className="page-title">{task.title}</h1>
        <div className="task-detail-badges">
          <StatusBadge status={task.status} />
          <PriorityBadge priority={task.priority} />
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="task-detail-grid">
        <div>
          <p className="task-description">{task.description || "No description provided."}</p>

          {canUpdateStatus && (
            <div className="status-changer">
              <span>Update status:</span>
              {STATUSES.map((s) => (
                <button
                  key={s}
                  className={`btn btn-small ${task.status === s ? "btn-primary" : "btn-ghost"}`}
                  onClick={() => handleStatusChange(s)}
                  disabled={task.status === s}
                >
                  {s}
                </button>
              ))}
            </div>
          )}
        </div>

        <aside className="task-meta-panel">
          <div>
            <span className="meta-label">Assignee</span>
            <span>{task.assignedToName || "Unassigned"}</span>
          </div>
          <div>
            <span className="meta-label">Created by</span>
            <span>{task.createdByName}</span>
          </div>
          <div>
            <span className="meta-label">Team</span>
            <span>{task.teamName || "—"}</span>
          </div>
          <div>
            <span className="meta-label">Due date</span>
            <span>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : "—"}</span>
          </div>
          <div>
            <span className="meta-label">Created</span>
            <span>{new Date(task.createdAt).toLocaleString()}</span>
          </div>
        </aside>
      </div>

      <section className="panel">
        <h2>Comments ({comments.length})</h2>
        <div className="comment-list">
          {comments.length === 0 && <p className="empty-state">No comments yet.</p>}
          {comments.map((c) => (
            <div className="comment-item" key={c.id}>
              <div className="comment-item-header">
                <strong>{c.userName}</strong>
                <span>{new Date(c.createdAt).toLocaleString()}</span>
              </div>
              <p>{c.content}</p>
            </div>
          ))}
        </div>

        <form className="comment-form" onSubmit={handleAddComment}>
          <textarea
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Write a comment..."
            rows={2}
            maxLength={2000}
          />
          <button type="submit" className="btn btn-primary" disabled={posting || !newComment.trim()}>
            {posting ? "Posting..." : "Post comment"}
          </button>
        </form>
      </section>

      <Link to="/tasks" className="link-button">
        Back to task list
      </Link>
    </div>
  );
}
