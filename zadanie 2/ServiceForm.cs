using System.Drawing;
using System.Windows.Forms;

namespace zadanie_2;

public class ServiceForm : Form
{
    private readonly DatabaseHelper _db;
    private readonly FlowLayoutPanel _servicesPanel;
    private readonly Label _statusLabel;

    // Фильтры
    private TextBox _searchBox = null!;
    private ComboBox _discountFilter = null!;
    private Button _sortButton = null!;
    private Button _addButton = null!;
    private Button _bookButton = null!;
    private Button _refreshButton = null!;

    private bool _sortAsc = true;
    private string _sortBy = "ID";

    public ServiceForm(DatabaseHelper db)
    {
        _db = db;

        Text = "Услуги - izp-44_kuralenkov_rg_zadanie2";
        Size = new Size(900, 700);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        // Заголовок
        var titleLabel = new Label
        {
            Text = "Список услуг",
            Location = new Point(20, 15),
            Size = new Size(300, 30),
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 40, 40)
        };

        // Панель фильтров и поиска
        var filterPanel = new Panel
        {
            Location = new Point(20, 55),
            Size = new Size(840, 70),
            BackColor = Color.FromArgb(245, 245, 245),
            BorderStyle = BorderStyle.FixedSingle
        };

        var searchLabel = new Label
        {
            Text = "Поиск по названию:",
            Location = new Point(10, 10),
            Size = new Size(110, 20),
            Font = new Font("Segoe UI", 9)
        };
        _searchBox = new TextBox
        {
            Location = new Point(10, 32),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 9)
        };
        _searchBox.TextChanged += async (_, _) => await LoadServicesAsync();

        var discountLabel = new Label
        {
            Text = "Скидка:",
            Location = new Point(220, 10),
            Size = new Size(60, 20),
            Font = new Font("Segoe UI", 9)
        };
        _discountFilter = new ComboBox
        {
            Location = new Point(220, 32),
            Size = new Size(150, 25),
            Font = new Font("Segoe UI", 9),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _discountFilter.Items.AddRange(new object[]
        {
            "Все",
            "0-5%",
            "5-15%",
            "15-30%",
            "30-70%",
            "70-100%"
        });
        _discountFilter.SelectedIndex = 0;
        _discountFilter.SelectedIndexChanged += async (_, _) => await LoadServicesAsync();

        _sortButton = new Button
        {
            Text = "Сортировка: ID ↑",
            Location = new Point(380, 32),
            Size = new Size(120, 28),
            Font = new Font("Segoe UI", 9)
        };
        _sortButton.Click += async (_, _) =>
        {
            _sortAsc = !_sortAsc;
            _sortButton.Text = $"Сортировка: ID {(_sortAsc ? "↑" : "↓")}";
            await LoadServicesAsync();
        };

        filterPanel.Controls.Add(searchLabel);
        filterPanel.Controls.Add(_searchBox);
        filterPanel.Controls.Add(discountLabel);
        filterPanel.Controls.Add(_discountFilter);
        filterPanel.Controls.Add(_sortButton);

        // Кнопки действий
        var actionPanel = new Panel
        {
            Location = new Point(20, 130),
            Size = new Size(840, 35),
            BackColor = Color.Transparent
        };

        _addButton = new Button
        {
            Text = "Добавить услугу",
            Location = new Point(0, 5),
            Size = new Size(120, 30),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _addButton.FlatAppearance.BorderSize = 0;
        _addButton.Click += async (_, _) => await AddServiceAsync();

        _bookButton = new Button
        {
            Text = "Записать клиента",
            Location = new Point(130, 5),
            Size = new Size(130, 30),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _bookButton.FlatAppearance.BorderSize = 0;
        _bookButton.Click += async (_, _) => await BookClientAsync();

        _refreshButton = new Button
        {
            Text = "Обновить",
            Location = new Point(720, 5),
            Size = new Size(100, 30),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.WhiteSmoke,
            FlatStyle = FlatStyle.Flat
        };
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.FlatAppearance.BorderColor = Color.Gray;
        _refreshButton.Click += async (_, _) => await LoadServicesAsync();

        actionPanel.Controls.Add(_addButton);
        actionPanel.Controls.Add(_bookButton);
        actionPanel.Controls.Add(_refreshButton);

        // Статус
        _statusLabel = new Label
        {
            Text = "Загрузка...",
            Location = new Point(20, 640),
            Size = new Size(840, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray
        };

        // Панель с услугами (автоскролл)
        _servicesPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 170),
            Size = new Size(840, 460),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.White
        };

        Controls.Add(titleLabel);
        Controls.Add(filterPanel);
        Controls.Add(actionPanel);
        Controls.Add(_servicesPanel);
        Controls.Add(_statusLabel);

        Load += async (_, _) => await LoadServicesAsync();
    }

    private async Task LoadServicesAsync()
    {
        _statusLabel.Text = "Загрузка услуг из базы данных...";
        _servicesPanel.Controls.Clear();

        try
        {
            string? search = string.IsNullOrWhiteSpace(_searchBox.Text) ? null : _searchBox.Text;

            double? minDisc = null, maxDisc = null;
            switch (_discountFilter.SelectedIndex)
            {
                case 1: minDisc = 0; maxDisc = 5; break;
                case 2: minDisc = 5; maxDisc = 15; break;
                case 3: minDisc = 15; maxDisc = 30; break;
                case 4: minDisc = 30; maxDisc = 70; break;
                case 5: minDisc = 70; maxDisc = 100; break;
            }

            var services = await _db.GetServicesAsync(search, minDisc, maxDisc, _sortBy, _sortAsc);

            foreach (var service in services)
            {
                var card = new ServiceCard(service);
                card.EditRequested += async id => await EditServiceAsync(id);
                card.DeleteRequested += async id => await DeleteServiceAsync(id);
                _servicesPanel.Controls.Add(card);
            }

            _statusLabel.Text = $"Загружено услуг: {services.Count}";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Ошибка: {ex.Message}";
            MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AddServiceAsync()
    {
        var newService = new ServiceDto(0, "", 0, 0, null, null, null);
        using var editForm = new EditServiceForm(newService, isNew: true);
        if (editForm.ShowDialog() == DialogResult.OK)
        {
            var success = await _db.AddServiceAsync(editForm.Service);
            if (success)
            {
                MessageBox.Show("Услуга успешно добавлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadServicesAsync();
            }
            else
            {
                MessageBox.Show("Ошибка при добавлении услуги. Проверьте: уникальность названия, длительность (не более 4 часов, не отрицательная).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task EditServiceAsync(int serviceId)
    {
        // Получаем текущие данные (включая отфильтрованные)
        string? search = string.IsNullOrWhiteSpace(_searchBox.Text) ? null : _searchBox.Text;
        double? minDisc = null, maxDisc = null;
        switch (_discountFilter.SelectedIndex)
        {
            case 1: minDisc = 0; maxDisc = 5; break;
            case 2: minDisc = 5; maxDisc = 15; break;
            case 3: minDisc = 15; maxDisc = 30; break;
            case 4: minDisc = 30; maxDisc = 70; break;
            case 5: minDisc = 70; maxDisc = 100; break;
        }
        var services = await _db.GetServicesAsync(search, minDisc, maxDisc, _sortBy, _sortAsc);
        var service = services.FirstOrDefault(s => s.ID == serviceId);
        if (service == null) return;

        using var editForm = new EditServiceForm(service, isNew: false);
        if (editForm.ShowDialog() == DialogResult.OK)
        {
            var updated = editForm.Service;
            var success = await _db.UpdateServiceAsync(updated);
            if (success)
            {
                MessageBox.Show("Услуга успешно обновлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadServicesAsync();
            }
            else
            {
                MessageBox.Show("Ошибка при обновлении услуги. Проверьте: уникальность названия, длительность (не более 4 часов, не отрицательная).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task DeleteServiceAsync(int serviceId)
    {
        var result = MessageBox.Show("Вы уверены, что хотите удалить эту услугу?",
            "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            var success = await _db.DeleteServiceAsync(serviceId);
            if (success)
            {
                MessageBox.Show("Услуга успешно удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadServicesAsync();
            }
            else
            {
                MessageBox.Show("Ошибка при удалении услуги", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task BookClientAsync()
    {
        // Получаем список услуг для выбора (с текущими фильтрами)
        string? search = string.IsNullOrWhiteSpace(_searchBox.Text) ? null : _searchBox.Text;
        double? minDisc = null, maxDisc = null;
        switch (_discountFilter.SelectedIndex)
        {
            case 1: minDisc = 0; maxDisc = 5; break;
            case 2: minDisc = 5; maxDisc = 15; break;
            case 3: minDisc = 15; maxDisc = 30; break;
            case 4: minDisc = 30; maxDisc = 70; break;
            case 5: minDisc = 70; maxDisc = 100; break;
        }
        var services = await _db.GetServicesAsync(search, minDisc, maxDisc, _sortBy, _sortAsc);
        var clients = await _db.GetClientsAsync();

        if (services.Count == 0)
        {
            MessageBox.Show("Нет доступных услуг для записи", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (clients.Count == 0)
        {
            MessageBox.Show("Нет клиентов в базе данных", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var bookForm = new ClientBookingForm(services, clients);
        if (bookForm.ShowDialog() == DialogResult.OK)
        {
            var (clientId, serviceId, startTime, comment) = bookForm.BookingData;
            var success = await _db.CreateClientServiceAsync(clientId, serviceId, startTime, comment);
            if (success)
            {
                MessageBox.Show("Клиент успешно записан на услугу", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Ошибка при записи клиента на услугу", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

public class EditServiceForm : Form
{
    public ServiceDto Service { get; private set; }

    private readonly TextBox _titleBox;
    private readonly TextBox _costBox;
    private readonly TextBox _durationBox;
    private readonly TextBox _descriptionBox;
    private readonly TextBox _discountBox;
    private readonly TextBox _imagePathBox;
    private readonly Label _idLabel;
    private readonly bool _isNew;

    public EditServiceForm(ServiceDto service, bool isNew)
    {
        Service = service;
        _isNew = isNew;

        Text = _isNew ? "Добавление услуги" : "Редактирование услуги";
        Size = new Size(500, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var y = 20;

        if (_isNew)
        {
            var idNote = new Label
            {
                Text = "Идентификатор будет назначен автоматически",
                Location = new Point(20, y),
                Size = new Size(440, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            Controls.Add(idNote);
            y += 30;
        }
        else
        {
            var idLabel = new Label { Text = "Идентификатор:", Location = new Point(20, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
            _idLabel = new Label { Text = service.ID.ToString(), Location = new Point(130, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
            Controls.Add(idLabel);
            Controls.Add(_idLabel);
            y += 50;
        }

        Action<string, string> addField = (label, defaultValue) =>
        {
            var lbl = new Label { Text = label, Location = new Point(20, y), Size = new Size(150, 20), Font = new Font("Segoe UI", 10) };
            var txt = new TextBox { Location = new Point(20, y + 25), Size = new Size(440, 25), Text = defaultValue, Font = new Font("Segoe UI", 10) };
            Controls.Add(lbl);
            Controls.Add(txt);
            y += 60;
        };

        addField("Название услуги:", service.Title);
        _titleBox = (TextBox)Controls[^1];

        addField("Стоимость:", service.Cost.ToString("F2"));
        _costBox = (TextBox)Controls[^1];

        addField("Длительность (секунды):", service.DurationInSeconds.ToString());
        _durationBox = (TextBox)Controls[^1];

        addField("Описание:", service.Description ?? "");
        _descriptionBox = (TextBox)Controls[^1];

        addField("Скидка (%):", service.Discount?.ToString("F0") ?? "");
        _discountBox = (TextBox)Controls[^1];

        addField("Путь к изображению:", service.MainImagePath ?? "");
        _imagePathBox = (TextBox)Controls[^1];

        var saveButton = new Button
        {
            Text = _isNew ? "Добавить" : "Сохранить",
            Location = new Point(280, y + 10),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(390, y + 10),
            Size = new Size(90, 35),
            DialogResult = DialogResult.Cancel,
            Font = new Font("Segoe UI", 10)
        };
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        saveButton.Click += (_, _) =>
        {
            Service = service with
            {
                Title = _titleBox.Text.Trim(),
                Cost = decimal.TryParse(_costBox.Text, out var c) ? c : service.Cost,
                DurationInSeconds = int.TryParse(_durationBox.Text, out var d) ? d : service.DurationInSeconds,
                Description = string.IsNullOrWhiteSpace(_descriptionBox.Text) ? null : _descriptionBox.Text.Trim(),
                Discount = string.IsNullOrWhiteSpace(_discountBox.Text) ? (double?)null : double.Parse(_discountBox.Text),
                MainImagePath = string.IsNullOrWhiteSpace(_imagePathBox.Text) ? null : _imagePathBox.Text.Trim()
            };
        };
    }
}

public class ClientBookingForm : Form
{
    public (int ClientID, int ServiceID, DateTime StartTime, string? Comment) BookingData { get; private set; }

    private readonly ComboBox _clientCombo;
    private readonly ComboBox _serviceCombo;
    private readonly DateTimePicker _datePicker;
    private readonly TextBox _timeBox;
    private readonly TextBox _commentBox;

    public ClientBookingForm(List<ServiceDto> services, List<ClientDto> clients)
    {
        Text = "Запись клиента на услугу";
        Size = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var y = 20;

        // Клиент
        var clientLabel = new Label { Text = "Клиент:", Location = new Point(20, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
        _clientCombo = new ComboBox
        {
            Location = new Point(20, y + 25),
            Size = new Size(440, 25),
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var c in clients)
            _clientCombo.Items.Add(new ClientItem(c.ID, $"{c.LastName} {c.FirstName} {c.Patronymic} - {c.Phone}"));
        _clientCombo.SelectedIndex = 0;
        Controls.Add(clientLabel);
        Controls.Add(_clientCombo);
        y += 60;

        // Услуга
        var serviceLabel = new Label { Text = "Услуга:", Location = new Point(20, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
        _serviceCombo = new ComboBox
        {
            Location = new Point(20, y + 25),
            Size = new Size(440, 25),
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var s in services)
            _serviceCombo.Items.Add(new ServiceItem(s.ID, $"{s.Title} - {s.Cost} руб."));
        _serviceCombo.SelectedIndex = 0;
        Controls.Add(serviceLabel);
        Controls.Add(_serviceCombo);
        y += 60;

        // Дата
        var dateLabel = new Label { Text = "Дата записи:", Location = new Point(20, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
        _datePicker = new DateTimePicker
        {
            Location = new Point(20, y + 25),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10)
        };
        Controls.Add(dateLabel);
        Controls.Add(_datePicker);
        y += 60;

        // Время
        var timeLabel = new Label { Text = "Время начала (ЧЧ:ММ):", Location = new Point(20, y), Size = new Size(160, 20), Font = new Font("Segoe UI", 10) };
        _timeBox = new TextBox
        {
            Location = new Point(20, y + 25),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10),
            Text = DateTime.Now.ToString("HH:mm")
        };
        Controls.Add(timeLabel);
        Controls.Add(_timeBox);
        y += 60;

        // Комментарий
        var commentLabel = new Label { Text = "Комментарий:", Location = new Point(20, y), Size = new Size(100, 20), Font = new Font("Segoe UI", 10) };
        _commentBox = new TextBox
        {
            Location = new Point(20, y + 25),
            Size = new Size(440, 25),
            Font = new Font("Segoe UI", 10)
        };
        Controls.Add(commentLabel);
        Controls.Add(_commentBox);
        y += 60;

        var saveButton = new Button
        {
            Text = "Записать",
            Location = new Point(280, y + 10),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(390, y + 10),
            Size = new Size(90, 35),
            DialogResult = DialogResult.Cancel,
            Font = new Font("Segoe UI", 10)
        };
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        saveButton.Click += (_, _) =>
        {
            var clientItem = (ClientItem)_clientCombo.SelectedItem!;
            var serviceItem = (ServiceItem)_serviceCombo.SelectedItem!;

            if (!DateTime.TryParse($"{_datePicker.Value:yyyy-MM-dd} {_timeBox.Text}", out var startTime))
            {
                MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BookingData = (clientItem.ID, serviceItem.ID, startTime, string.IsNullOrWhiteSpace(_commentBox.Text) ? null : _commentBox.Text.Trim());
        };
    }

    private record ClientItem(int ID, string Display)
    {
        public override string ToString() => Display;
    }

    private record ServiceItem(int ID, string Display)
    {
        public override string ToString() => Display;
    }
}
