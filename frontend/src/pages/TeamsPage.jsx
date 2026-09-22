import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import * as teamsApi from "../api/teams";
import { getUsers } from "../api/users";
import { extractErrorMessage } from "../api/client";
import TeamFormModal from "../components/TeamFormModal";
import ConfirmDialog from "../components/ConfirmDialog";

export default function TeamsPage() {
  const { user } = useAuth();
  const isAdmin = user.role === "Admin";
  const isManager = user.role === "Manager";
  const canManageUsers = isAdmin || isManager;

  const [teams, setTeams] = useState([]);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [showForm, setShowForm] = useState(false);
  const [editingTeam, setEditingTeam] = useState(null);
  const [deletingTeam, setDeletingTeam] = useState(null);
  const [addingMemberTo, setAddingMemberTo] = useState(null);
  const [selectedUserId, setSelectedUserId] = useState("");

  const load = () => {
    setLoading(true);
    teamsApi
      .getTeams()
      .then(setTeams)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  useEffect(() => {
    if (canManageUsers) {
      getUsers().then(setUsers).catch(() => {});
    }
  }, [canManageUsers]);

  const managers = users.filter((u) => u.role === "Manager");

  const canManageTeam = (team) => isAdmin || (isManager && team.managerId === user.id);

  const handleCreate = async (payload) => {
    await teamsApi.createTeam(payload);
    load();
  };

  const handleUpdate = async (payload) => {
    await teamsApi.updateTeam(editingTeam.id, payload);
    load();
  };

  const handleDelete = async () => {
    try {
      await teamsApi.deleteTeam(deletingTeam.id);
      setDeletingTeam(null);
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleAddMember = async (e) => {
    e.preventDefault();
    if (!selectedUserId) return;
    try {
      await teamsApi.addTeamMember(addingMemberTo.id, selectedUserId);
      setAddingMemberTo(null);
      setSelectedUserId("");
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleRemoveMember = async (team, memberId) => {
    try {
      await teamsApi.removeTeamMember(team.id, memberId);
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  if (loading) return <div className="page-loading">Loading teams...</div>;

  return (
    <div>
      <div className="page-header-row">
        <h1 className="page-title">Teams</h1>
        {isAdmin && (
          <button className="btn btn-primary" onClick={() => setShowForm(true)}>
            + New team
          </button>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {teams.length === 0 ? (
        <p className="empty-state">No teams yet.</p>
      ) : (
        <div className="team-grid">
          {teams.map((team) => (
            <div className="team-card" key={team.id}>
              <div className="team-card-header">
                <h3>{team.name}</h3>
                {canManageTeam(team) && (
                  <div className="row-actions">
                    {isAdmin && (
                      <>
                        <button className="link-button" onClick={() => setEditingTeam(team)}>
                          Edit
                        </button>
                        <button className="link-button danger" onClick={() => setDeletingTeam(team)}>
                          Delete
                        </button>
                      </>
                    )}
                  </div>
                )}
              </div>
              {team.description && <p className="team-description">{team.description}</p>}
              <p className="team-manager">Manager: {team.managerName || "Unassigned"}</p>

              <div className="team-members">
                <div className="team-members-header">
                  <span>Members ({team.memberCount})</span>
                  {canManageTeam(team) && (
                    <button className="link-button" onClick={() => setAddingMemberTo(team)}>
                      + Add member
                    </button>
                  )}
                </div>
                <ul>
                  {team.members.map((m) => (
                    <li key={m.id}>
                      <span>
                        {m.fullName} <em>({m.role})</em>
                      </span>
                      {canManageTeam(team) && m.id !== team.managerId && (
                        <button className="link-button danger" onClick={() => handleRemoveMember(team, m.id)}>
                          Remove
                        </button>
                      )}
                    </li>
                  ))}
                  {team.members.length === 0 && <li className="empty-state">No members yet.</li>}
                </ul>
              </div>
            </div>
          ))}
        </div>
      )}

      {showForm && (
        <TeamFormModal managers={managers} onClose={() => setShowForm(false)} onSubmit={handleCreate} />
      )}

      {editingTeam && (
        <TeamFormModal
          team={editingTeam}
          managers={managers}
          onClose={() => setEditingTeam(null)}
          onSubmit={handleUpdate}
        />
      )}

      {deletingTeam && (
        <ConfirmDialog
          title="Delete team"
          message={`Delete "${deletingTeam.name}"? Members will be unassigned.`}
          confirmLabel="Delete"
          onCancel={() => setDeletingTeam(null)}
          onConfirm={handleDelete}
        />
      )}

      {addingMemberTo && (
        <div className="modal-overlay" onClick={() => setAddingMemberTo(null)}>
          <form className="modal-card modal-small" onClick={(e) => e.stopPropagation()} onSubmit={handleAddMember}>
            <h2>Add member to {addingMemberTo.name}</h2>
            <label>
              User
              <select value={selectedUserId} onChange={(e) => setSelectedUserId(e.target.value)} required>
                <option value="">Select a user</option>
                {users
                  .filter((u) => u.teamId !== addingMemberTo.id)
                  .map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.fullName} ({u.role})
                    </option>
                  ))}
              </select>
            </label>
            <div className="modal-actions">
              <button type="button" className="btn btn-ghost" onClick={() => setAddingMemberTo(null)}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary">
                Add
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
