using System.Drawing;
using System.Windows.Forms;

namespace zadanie_2;

public class ServiceCard : Panel
{
    private readonly PictureBox _imageBox;
    private readonly Label _titleLabel;
    private readonly Label _priceLabel;
    private readonly Label _discountLabel;
    private readonly Button _editButton;
    private readonly Button _deleteButton;

    public int ServiceId { get; }
    public event Action<int>? EditRequested;
    public event Action<int>? DeleteRequested;

    public ServiceCard(ServiceDto service)
    {
        ServiceId = service.ID;

        Size = new Size(760, 110);
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.White;
        Margin = new Padding(5);

        // Картинка услуги
        _imageBox = new PictureBox
        {
            Size = new Size(200, 100),
            Location = new Point(10, 5),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = service.ID % 2 == 0 ? Color.LightBlue : Color.LightPink,
            BorderStyle = BorderStyle.FixedSingle
        };
        // Пытаемся загрузить изображение по пути из БД
        if (!string.IsNullOrEmpty(service.MainImagePath))
        {
            try
            {
                var path = service.MainImagePath.Replace('\\', '/');
                // Проверяем локальные изображения из папки extracted_reqs
                var localPath = Path.Combine(
                    @"C:\Users\dayn\Desktop\drive-download-20261122T150421Z-1-001\Сессия 1\extracted_reqs\Услуги школы",
                    Path.GetFileName(path));
                if (File.Exists(localPath))
                    _imageBox.Image = Image.FromFile(localPath);
                else if (File.Exists(path))
                    _imageBox.Image = Image.FromFile(path);
            }
            catch { /* используем цветной фон */ }
        }

        // Название услуги
        _titleLabel = new Label
        {
            Text = service.Title,
            Location = new Point(225, 15),
            Size = new Size(520, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        // Цена и время
        var durationMinutes = service.DurationInSeconds / 60;
        _priceLabel = new Label
        {
            Text = $"{service.Cost:F0} рублей за {durationMinutes} минут",
            Location = new Point(225, 55),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

        // Скидка
        _discountLabel = new Label
        {
            Text = service.Discount.HasValue ? $"* скидка {service.Discount.Value:F0}%" : string.Empty,
            Location = new Point(225, 80),
            Size = new Size(200, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.Gray,
            Visible = service.Discount.HasValue
        };

        // Кнопка редактировать
        _editButton = new Button
        {
            Text = "редактировать",
            Location = new Point(540, 55),
            Size = new Size(110, 30),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            BackColor = Color.WhiteSmoke,
            FlatStyle = FlatStyle.Flat
        };
        _editButton.FlatStyle = FlatStyle.Flat;
        _editButton.FlatAppearance.BorderColor = Color.Gray;
        _editButton.Click += (_, _) => EditRequested?.Invoke(ServiceId);

        // Кнопка удалить
        _deleteButton = new Button
        {
            Text = "удалить",
            Location = new Point(660, 55),
            Size = new Size(80, 30),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            BackColor = Color.WhiteSmoke,
            FlatStyle = FlatStyle.Flat
        };
        _deleteButton.FlatStyle = FlatStyle.Flat;
        _deleteButton.FlatAppearance.BorderColor = Color.Gray;
        _deleteButton.Click += (_, _) => DeleteRequested?.Invoke(ServiceId);

        Controls.Add(_imageBox);
        Controls.Add(_titleLabel);
        Controls.Add(_priceLabel);
        Controls.Add(_discountLabel);
        Controls.Add(_editButton);
        Controls.Add(_deleteButton);
    }

    public void UpdateData(ServiceDto service)
    {
        _titleLabel.Text = service.Title;
        var durationMinutes = service.DurationInSeconds / 60;
        _priceLabel.Text = $"{service.Cost:F0} рублей за {durationMinutes} минут";
        _discountLabel.Text = service.Discount.HasValue ? $"* скидка {service.Discount.Value:F0}%" : string.Empty;
        _discountLabel.Visible = service.Discount.HasValue;
    }
}
