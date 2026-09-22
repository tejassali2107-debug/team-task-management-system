import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import * as usersApi from "../api/users";
import { getTeams } from "../api/teams";
import { extractErrorMessage } from "../api/client";
import UserFormModal from "../components/UserFormModal";
import ConfirmDialog from "../components/ConfirmDialog";

const ROLES = ["Admin", "Manager", "User"];

export default function UsersPage() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [teams, setTeams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [deletingUser, setDeletingUser] = useState(null);

  const load = () => {
    setLoading(true);
    usersApi
      .getUsers()
      .then(setUsers)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);
  useEffect(() => {
    getTeams().then(setTeams).catch(() => {});
  }, []);

  const handleCreate = async (payload) => {
    await usersApi.createUser(payload);
    load();
  };

  const handleRoleChange = async (user, role) => {
    try {
      await usersApi.updateUser(user.id, { role, teamId: user.teamId });
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleTeamChange = async (user, teamId) => {
    try {
      await usersApi.updateUser(user.id, { role: user.role, teamId: teamId || null });
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  const handleDelete = async () => {
    try {
      await usersApi.deleteUser(deletingUser.id);
      setDeletingUser(null);
      load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  if (loading) return <div className="page-loading">Loading users...</div>;

  return (
    <div>
      <div className="page-header-row">
        <h1 className="page-title">Users</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + New user
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Team</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.fullName}</td>
              <td>{u.email}</td>
              <td>
                <select value={u.role} onChange={(e) => handleRoleChange(u, e.target.value)}>
                  {ROLES.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
              </td>
              <td>
                <select value={u.teamId || ""} onChange={(e) => handleTeamChange(u, e.target.value)}>
                  <option value="">No team</option>
                  {teams.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </td>
              <td>
                {u.id !== currentUser.id && (
                  <button className="link-button danger" onClick={() => setDeletingUser(u)}>
                    Delete
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {showForm && (
        <UserFormModal teams={teams} onClose={() => setShowForm(false)} onSubmit={handleCreate} />
      )}

      {deletingUser && (
        <ConfirmDialog
          title="Delete user"
          message={`Delete "${deletingUser.fullName}"? This cannot be undone.`}
          confirmLabel="Delete"
          onCancel={() => setDeletingUser(null)}
          onConfirm={handleDelete}
        />
      )}
    </div>
  );
}
