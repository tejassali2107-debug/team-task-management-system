import { useState } from "react";
import { extractErrorMessage } from "../api/client";

export default function TeamFormModal({ team, managers, onClose, onSubmit }) {
  const isEdit = !!team;
  const [name, setName] = useState(team?.name || "");
  const [description, setDescription] = useState(team?.description || "");
  const [managerId, setManagerId] = useState(team?.managerId || "");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSaving(true);
    try {
      await onSubmit({ name, description: description || null, managerId: managerId || null });
      onClose();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <form className="modal-card" onClick={(e) => e.stopPropagation()} onSubmit={handleSubmit}>
        <h2>{isEdit ? "Edit team" : "New team"}</h2>
        {error && <div className="alert alert-error">{error}</div>}

        <label>
          Team name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={150} />
        </label>

        <label>
          Description
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            maxLength={500}
          />
        </label>

        <label>
          Manager
          <select value={managerId} onChange={(e) => setManagerId(e.target.value)}>
            <option value="">No manager assigned</option>
            {managers.map((m) => (
              <option key={m.id} value={m.id}>
                {m.fullName}
              </option>
            ))}
          </select>
        </label>

        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? "Saving..." : isEdit ? "Save changes" : "Create team"}
          </button>
        </div>
      </form>
    </div>
  );
}
