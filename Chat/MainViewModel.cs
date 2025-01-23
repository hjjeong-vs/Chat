using Chat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.Json;
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace Chat
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //Chat Type
        public enum C_TYPE
        {
            MESSAGE = 0,
            IMAGE,
            LIST,
            JOIN
        }

        public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        private string _message = "";

        public string MessageToSend
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(MessageToSend));
            }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand UpdateNicknameCommand { get; }

        public ObservableCollection<string> ClientList { get; set; } = new ObservableCollection<string>();


        private Socket mainSock;

        public MainViewModel()
        {
            SendMessageCommand = new RelayCommand(SendMessage);
            SendImageCommand = new RelayCommand(SendImage);
            UpdateNicknameCommand = new RelayCommand(UpdateNickname);

            SetupConnection();
        }

        private void SetupConnection()
        {
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mainSock.Connect("127.0.0.1", 12345);

            var obj = new AsyncObject(5000);
            obj.WorkingSocket = mainSock;
            mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }

        private void DataReceived(IAsyncResult ar)
        {
            var obj = (AsyncObject)ar.AsyncState;
            int received = obj.WorkingSocket.EndReceive(ar);

            if (received > 0)
            {
                string temp = Encoding.UTF8.GetString(obj.Buffer, 0, received);
                ReceivePacket packet = JsonSerializer.Deserialize<ReceivePacket>(temp);
                string strMessage = "";
                switch (packet.type)
                {
                    case (int)C_TYPE.MESSAGE:
                        strMessage = "[" + packet.date.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
                        strMessage += packet.Name.Trim() + ": ";
                        strMessage += packet.Message.TrimEnd();
                        break;

                    case (int)C_TYPE.IMAGE:
                        strMessage = "[" + packet.date.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
                        strMessage += packet.Name.Trim() + ": ";
                        strMessage += "이미지 저장";

                        BitmapImage bit = Base64ToImage(packet.Message);
                        string currentPath = AppDomain.CurrentDomain.BaseDirectory;

                        SaveBitmapImage(bit, currentPath + "image" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg");
                        break;

                }

                App.Current.Dispatcher.Invoke(() => ClientList.Clear());
                for (int i = 0; i < packet.clients.Count; i++)
                {
                    App.Current.Dispatcher.Invoke(() => ClientList.Add(packet.clients[i]));
                }

                App.Current.Dispatcher.Invoke(() => Messages.Add(strMessage));
            }

            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }
        private void SaveBitmapImage(BitmapImage bitmapImage, string filePath)
        {
            BitmapFrame frame = BitmapFrame.Create(bitmapImage);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
        public void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageToSend)) return;

            var data = MakeSendMessage(MessageToSend, (int)C_TYPE.MESSAGE);
            mainSock.Send(data);
            MessageToSend = "";
        }

        private byte[] MakeSendMessage(string strMsg, int cType)
        {
            MessagePacket packet = new MessagePacket();
            packet.type = cType;
            packet.Message = strMsg;
            packet.Name = LabelName;
            packet.date = DateTime.Now;

            string strJson = JsonSerializer.Serialize(packet);

            var data = Encoding.UTF8.GetBytes(strJson);

            return data;

        }

        private string ImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);

            string base64String = Convert.ToBase64String(imageBytes);

            return base64String;
        }

        private BitmapImage Base64ToImage(string base64String)
        {
            // Base64 문자열을 byte 배열로 변환
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // MemoryStream을 이용해 BitmapImage 생성
            using (var memoryStream = new MemoryStream(imageBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }

        private void SendImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string strBase64 = ImageToBase64(selectedFilePath);
                var data = MakeSendMessage(strBase64, (int)C_TYPE.IMAGE);
                mainSock.Send(data);
            }
           
        }

        private string _inputName = "익명";
        private string _labelName = "익명";

        public string InputName
        {
            get => _inputName;
            set
            {
                _inputName = value;
                OnPropertyChanged(nameof(InputName));
            }
        }

        public string LabelName
        {
            get => _labelName;
            set
            {
                _labelName = value;
                OnPropertyChanged(nameof(LabelName));
            }
        }



        public void UpdateNickname()
        {
            LabelName = InputName;
            InputName = "";
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged;
    }

    public class AsyncObject
    {
        public byte[] Buffer { get; private set; }
        public Socket WorkingSocket { get; set; }
        public int BufferSize { get; private set; }

        public AsyncObject(int bufferSize)
        {
            Buffer = new byte[bufferSize];
            BufferSize = bufferSize;
        }
    }
}
