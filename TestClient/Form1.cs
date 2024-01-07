using NewLife;
using System.Xml.Linq;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;

namespace TestClient
{
    public partial class Form1 : Form
    {
        private SiemensS7Driver _driver;
        private SiemensNode _node;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_driver == null)
            {
                var driver = new SiemensS7Driver();
                var pm = new SiemensParameter
                {
                    Address = $"{tb_address.Text}:{tb_port.Text}",
                    CpuType = CpuType.S7200Smart,
                    Rack = 0,
                    Slot = 2,
                };

                _node = driver.Open(null, pm) as SiemensNode;
                if (_node is SiemensNode node)
                {
                    _driver = driver;

                    this.Invoke(() =>
                    {
                        btn_conn.Text = "断开";
                        rtb_content.Append("连接成功");
                        rtb_content.Append("\r\n");
                    });
                }
                else
                {
                    this.Invoke(() =>
                    {
                        rtb_content.Append("连接失败");
                        rtb_content.Append("\r\n");
                    });
                }
            }
            else
            {
                _driver.Dispose();
                _driver = null;

                this.Invoke(() =>
                {
                    btn_conn.Text = "连接";

                    rtb_content.Append("断开链接");
                    rtb_content.Append("\r\n");
                });
            }
        }

        private void btn_write_Click(object sender, EventArgs e)
        {
            var pointAdd = tb_pointAddress.Text;
            var value = tb_value.Text.ToInt();
            var length = tb_length.Text.ToInt();
            var type = tb_type.Text;

            var point = new Point
            {
                Name = "污泥泵停止时间",
                Address = pointAdd, // "M100",
                Type = type,
                Length = length //data.Length
            };

            var data = BitConverter.GetBytes(value);

            try
            {
                _driver.Write(_node, point, value);

                this.Invoke(() =>
                {
                    rtb_content.Append($"点位：{pointAdd},值：{value},长度：{length}");
                    rtb_content.Append("\r\n");
                    rtb_content.Append("写入完成");
                    rtb_content.Append("\r\n");
                });
            }
            catch (Exception ex)
            {
                this.Invoke(() =>
                {
                    rtb_content.Append($"点位：{pointAdd},值：{value},长度：{length}");
                    rtb_content.Append("\r\n");
                    rtb_content.Append("写入失败");
                    rtb_content.Append("\r\n");
                    rtb_content.Append(ex + "");
                });
            }
        }

        private void btn_read_Click(object sender, EventArgs e)
        {
            var pointAdd = tb_pointAddress.Text;
            var value = tb_value.Text.ToInt();
            var length = tb_length.Text.ToInt();
            var type = tb_type.Text;

            var point = new Point
            {
                Name = "污泥泵停止时间",
                Address = pointAdd, // "M100",
                Type = type,
                Length = length //data.Length
            };

            try
            {
                // 读取
                var dic = _driver.Read(_node, new[] { point });
                var data1 = dic[point.Name] as Byte[];
                var res = data1.Swap(true, false).ToInt();

                this.Invoke(() =>
                {
                    rtb_content.Append($"点位：{pointAdd},值：{value},长度：{length}");
                    rtb_content.Append($"读取值：{res}");
                    rtb_content.Append("\r\n");
                });
            }
            catch (Exception ex)
            {
                this.Invoke(() =>
                {
                    rtb_content.Append($"点位：{pointAdd},值：{value},长度：{length}");
                    rtb_content.Append("读取失败");
                    rtb_content.Append("\r\n");
                    rtb_content.Append(ex + "");
                });
            }
        }
    }

    public class Point : IPoint
    {
        public String Name { get; set; }
        public String Address { get; set; }
        public String Type { get; set; }
        public Int32 Length { get; set; }
    }
}
