using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using Neo4jCrudApp.Data;
using Neo4jCrudApp.Models;

namespace Neo4jCrudApp
{
    public partial class MainForm : Form
    {
        private PersonRepository _repo;
        private string _selectedOriginalName; // tracks which row is selected, for Update/Delete

        public MainForm()
        {
            InitializeComponent();

            dgvPeople.Columns.Add("Name", "Name");
            dgvPeople.Columns.Add("Age", "Age");
            dgvPeople.Columns.Add("City", "City");
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            var uri = ConfigurationManager.AppSettings["Neo4jUri"];
            var user = ConfigurationManager.AppSettings["Neo4jUser"];
            var password = ConfigurationManager.AppSettings["Neo4jPassword"];
            var database = ConfigurationManager.AppSettings["Neo4jDatabase"];

            _repo = new PersonRepository(uri, user, password, database);

            try
            {
                SetStatus("Connecting...");
                await _repo.VerifyConnectivityAsync();
                SetStatus("Connected.");
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                SetStatus("Connection failed.");
                MessageBox.Show(this, $"Could not connect to Neo4j:\n{ex.Message}",
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------- CREATE ----------------
        private async void btnCreate_Click(object sender, EventArgs e)
        {
            if (!TryReadForm(out var person, out var error))
            {
                MessageBox.Show(this, error, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusy(true);
                await _repo.CreateAsync(person);
                SetStatus($"Created '{person.Name}'.");
                ClearForm();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ---------------- UPDATE ----------------
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedOriginalName == null)
            {
                MessageBox.Show(this, "Select a row in the grid first.", "No selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryReadForm(out var person, out var error))
            {
                MessageBox.Show(this, error, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusy(true);
                var updated = await _repo.UpdateAsync(_selectedOriginalName, person);
                SetStatus(updated ? $"Updated '{_selectedOriginalName}'." : "Nothing matched that name.");
                ClearForm();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ---------------- DELETE ----------------
        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedOriginalName == null)
            {
                MessageBox.Show(this, "Select a row in the grid first.", "No selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this, $"Delete all records?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                SetBusy(true);
                var deleted = await _repo.DeleteAllAsync();
                SetStatus(deleted ? $"Deleted all people." : "No people to delete.");
                ClearForm();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            if(_selectedOriginalName == null)
            {
                MessageBox.Show(this, "Select a row in the grid first.", "No selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this, $"Delete '{_selectedOriginalName}'?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                SetBusy(true);
                var deleted = await _repo.DeleteAllAsync();
                SetStatus(deleted ? $"Deleted all people." : "No people to delete.");
                ClearForm();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                SetBusy(false);
            }

        }
        // ---------------- READ (refresh grid) ----------------
        private async void btnRefresh_Click(object sender, EventArgs e) => await LoadGridAsync();

        private async System.Threading.Tasks.Task LoadGridAsync()
        {
            try
            {
                SetBusy(true);
                var people = await _repo.GetAllAsync();

                dgvPeople.Rows.Clear();
                foreach (var p in people)
                {
                    dgvPeople.Rows.Add(p.Name, p.Age, p.City);
                }
                SetStatus($"{people.Count} record(s) loaded.");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void dgvPeople_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPeople.SelectedRows.Count == 0)
            {
                _selectedOriginalName = null;
                return;
            }

            var row = dgvPeople.SelectedRows[0];
            _selectedOriginalName = row.Cells["Name"].Value?.ToString();

            txtName.Text = _selectedOriginalName;
            numAge.Value = Convert.ToInt32(row.Cells["Age"].Value ?? 0);
            txtCity.Text = row.Cells["City"].Value?.ToString() ?? "";
        }

        private void btnClear_Click(object sender, EventArgs e) => ClearForm();

        // ---------------- helpers ----------------
        private bool TryReadForm(out Person person, out string error)
        {
            person = new Person
            {
                Name = txtName.Text.Trim(),
                Age = (int)numAge.Value,
                City = txtCity.Text.Trim()
            };

            if (string.IsNullOrEmpty(person.Name))
            {
                error = "Name is required.";
                return false;
            }

            error = null;
            return true;
        }

        private void ClearForm()
        {
            txtName.Text = "";
            numAge.Value = 0;
            txtCity.Text = "";
            _selectedOriginalName = null;
            dgvPeople.ClearSelection();
        }

        private void SetStatus(string text) => lblStatus.Text = text;

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            btnCreate.Enabled = !busy;
            btnUpdate.Enabled = !busy;
            btnDelete.Enabled = !busy;
            btnRefresh.Enabled = !busy;
        }

        private void ShowError(Exception ex)
        {
            SetStatus("Error.");
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override async void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_repo != null)
            {
                await _repo.DisposeAsync();
            }
        }

        
    }
}
