partial class MainForm
{
  private System.ComponentModel.IContainer components = null;
  private TextBox txtTickerFilter;
  private PictureBox postImagePictureBox;
  private RichTextBox postTextTextBox;
  private Button btnNegative;
  private Button btnPositive;
  private Button btnNeutral;
  private Button btnSkip;

  // Добавленные контролы
  private TextBox txtCurrentCursor;
  private TextBox txtPostInserted;

  protected override void Dispose(bool disposing)
  {
    if (disposing && (components != null))
    {
      components.Dispose();
    }
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    txtTickerFilter = new TextBox();
    postImagePictureBox = new PictureBox();
    postTextTextBox = new RichTextBox();
    btnNegative = new Button();
    btnPositive = new Button();
    btnNeutral = new Button();
    btnSkip = new Button();
    txtCurrentCursor = new TextBox();
    txtPostInserted = new TextBox();
    ((System.ComponentModel.ISupportInitialize)postImagePictureBox).BeginInit();
    SuspendLayout();
    // 
    // txtTickerFilter
    // 
    txtTickerFilter.Location = new Point(769, 11);
    txtTickerFilter.Name = "txtTickerFilter";
    txtTickerFilter.PlaceholderText = "Фильтр по тикеру";
    txtTickerFilter.Size = new Size(300, 23);
    txtTickerFilter.TabIndex = 1;
    txtTickerFilter.TextChanged += TxtTickerFilter_TextChanged;
    // 
    // postImagePictureBox
    // 
    postImagePictureBox.Location = new Point(12, 40);
    postImagePictureBox.Name = "postImagePictureBox";
    postImagePictureBox.Size = new Size(678, 740);
    postImagePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
    postImagePictureBox.TabIndex = 3;
    postImagePictureBox.TabStop = false;
    // 
    // postTextTextBox
    // 
    postTextTextBox.Font = new Font("Segoe UI", 16F);
    postTextTextBox.Location = new Point(696, 40);
    postTextTextBox.Name = "postTextTextBox";
    postTextTextBox.ReadOnly = true;
    postTextTextBox.Size = new Size(1099, 740);
    postTextTextBox.TabIndex = 4;
    postTextTextBox.Text = "";
    // 
    // btnNegative
    // 
    btnNegative.Location = new Point(12, 786);
    btnNegative.Name = "btnNegative";
    btnNegative.Size = new Size(388, 23);
    btnNegative.TabIndex = 7;
    btnNegative.Text = "--------------";
    btnNegative.Click += BtnNegative_Click;
    // 
    // btnPositive
    // 
    btnPositive.Location = new Point(1407, 786);
    btnPositive.Name = "btnPositive";
    btnPositive.Size = new Size(388, 23);
    btnPositive.TabIndex = 8;
    btnPositive.Text = "++++++++++++++++";
    btnPositive.Click += BtnPositive_Click;
    // 
    // btnNeutral
    // 
    btnNeutral.Location = new Point(934, 786);
    btnNeutral.Name = "btnNeutral";
    btnNeutral.Size = new Size(442, 23);
    btnNeutral.TabIndex = 9;
    btnNeutral.Text = "NEITRAL";
    btnNeutral.Click += BtnNeutral_Click;
    // 
    // btnSkip
    // 
    btnSkip.Location = new Point(449, 786);
    btnSkip.Name = "btnSkip";
    btnSkip.Size = new Size(453, 23);
    btnSkip.TabIndex = 10;
    btnSkip.Text = "SKIIIIIIIIIIP";
    btnSkip.Click += BtnSkip_Click;
    // 
    // txtCurrentCursor
    // 
    txtCurrentCursor.Location = new Point(12, 11);
    txtCurrentCursor.Name = "txtCurrentCursor";
    txtCurrentCursor.ReadOnly = true;
    txtCurrentCursor.Size = new Size(176, 23);
    txtCurrentCursor.TabIndex = 0;
    txtCurrentCursor.TabStop = false;
    // 
    // txtPostInserted
    // 
    txtPostInserted.Location = new Point(1597, 11);
    txtPostInserted.Name = "txtPostInserted";
    txtPostInserted.ReadOnly = true;
    txtPostInserted.Size = new Size(198, 23);
    txtPostInserted.TabIndex = 2;
    txtPostInserted.TabStop = false;
    // 
    // MainForm
    // 
    ClientSize = new Size(1807, 821);
    Controls.Add(txtCurrentCursor);
    Controls.Add(txtTickerFilter);
    Controls.Add(postImagePictureBox);
    Controls.Add(postTextTextBox);
    Controls.Add(txtPostInserted);
    Controls.Add(btnNegative);
    Controls.Add(btnPositive);
    Controls.Add(btnNeutral);
    Controls.Add(btnSkip);
    Name = "MainForm";
    Text = "TPulseTrainer — Тренировка оценки эмоционального окраса постов";
    Load += MainForm_Load;
    ((System.ComponentModel.ISupportInitialize)postImagePictureBox).EndInit();
    ResumeLayout(false);
    PerformLayout();
  }
}
