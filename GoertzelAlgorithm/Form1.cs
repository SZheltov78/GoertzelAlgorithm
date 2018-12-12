using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Un4seen.Bass;

namespace GoertzelAlgorithm
{
    public partial class Form1 : Form
    {

        //====================================================================================
        static int RateK = 4; //во сколько раз пересемплировать (пересемплирование просто для ускорения работы, достаточно посчитать магнитуду на частоте в 8кГц даже)
        static int CalcCycles = 500; //сколько сделать выборок из массива данных
        static int length = CalcCycles * 2 * RateK; // длинна массива, требуемая для расчетов, *2 т.к. 2 канала, *RateK т.к. для пересемплирования нужно шагать по массиву кратно этому коэф.
        static short[] data = new short[length + 100]; // + 100 на всякий
        static bool FefChannel = true; //использовать ли разницу каналов (как обычно записаны dtmf метки на радио)

        static int stream;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stream = Bass.BASS_StreamCreateFile("test.mp3", 0, 0, BASSFlag.BASS_DEFAULT);
            Bass.BASS_ChannelPlay(stream, false);
            timer1.Enabled = true;
        }
      

        // получить магнитуду частоты
        static double GetMagnitude(int TARGET_FREQUENCY)
        {
            int step;
            double Q2 = 0;
            double Q1 = 0;
            double Q0 = 0;
            double resultat = 0;

            double SAMPLING_RATE = 44100 / RateK; //частота расчета

            step = 0;
            double coeff =2 * Math.Cos(2 * Math.PI * TARGET_FREQUENCY / SAMPLING_RATE); //коэф. для заданной частоты
            double prev_resultat = 0; //предыдущий результат

            for (int index = 0; index <= CalcCycles; index++) //расчет
            {
                if (FefChannel)//используется разница каналов
                {
                    Q0 = coeff * Q1 - Q2 + ( data[step] / 1000 - data[step + 1] / 1000 ); //   /1000 это эмпирически подобрано
                }
                else//исп. только один канал
                {
                    Q0 = coeff * Q1 - Q2 + data[step] / 1000;
                }

                step = step + RateK * 2; //шаг по массиву кратен 2 т.к. стерео, массив = левый|правый|левый|правый, и кратен пересемплированию

                Q2 = Q1;
                Q1 = Q0; //Q0 сохраняется для следующего цикла, чтобы в сл.цикле стать Q2

                resultat = Q1 * Q1 + Q2 * Q2 - coeff * Q1 * Q2; //сама формула

                if (prev_resultat < resultat)
                {
                    prev_resultat = resultat * 0.8; //  *0.8 - эмпирически, от него частично зависит чувствительность (острота пика магнитуды, если построить график)
                }
            }

            prev_resultat = prev_resultat * 0.0001; //* 0.0001 тоже подбором, чтобы на выходе нормальные цифры были
            return prev_resultat;
        }


//======================================================================================================================
        string last_simbol = "";//предыдущий символ
        string DTMF_word = ""; //сама метка (слово)

        int counter = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            double n_data, M697, M770, M852, M941, M1209, M1336, M1477, M1633;
            double max;
            string result, s, DTMF_symbol;           

            //получить массив
            n_data = Bass.BASS_ChannelGetData(stream, data, length * 2);           

            if (n_data < length * 2) return;   //если слишком мало получено, не рассчитать

            //молучаем магнитуды нужных частот
            M697 = GetMagnitude(697);
            M770 = GetMagnitude(770);
            M852 = GetMagnitude(852);
            M941 = GetMagnitude(941);
            M1209 = GetMagnitude(1209);
            M1336 = GetMagnitude(1336);
            M1477 = GetMagnitude(1477);
            M1633 = GetMagnitude(1633);

            max = 20; //чувствительность
            result = "";    
            
            //достаточна ли магнитуда
            s = (M697 > max) ? "1" : "0";
            result = result +s; //добавляем к результату
            
            s = (M770 > max) ? "1" : "0";
            result = result + s;

            s = (M852 > max) ? "1" : "0";
            result = result + s;

            s = (M941 > max) ? "1" : "0";
            result = result + s;

            s = (M1209 > max) ? "1" : "0";
            result = result + s;

            s = (M1336 > max) ? "1" : "0";
            result = result + s;

            s = (M1477 > max) ? "1" : "0";
            result = result + s;

            s = (M1633 > max) ? "1" : "0";
            result = result + s;
            
            DTMF_symbol = "";           

            //в итоге получается нечто похожее на битовое поле 
            switch (result)
            {
                case "10001000": //1 - есть частоста, 0 - нет частоты в спектре (в зависимости от того какие частоты есть определяется символ)
                DTMF_symbol = "1";
                break;
                case "10000100":
                    DTMF_symbol = "2";                    
                    break;
                case "10000010":
                    DTMF_symbol = "3";
                    break;
                case "01001000":
                    DTMF_symbol = "4";
                    break;
                case "01000100":
                    DTMF_symbol = "5";
                    break;
                case "01000010":
                    DTMF_symbol = "6";
                    break;
                case "00101000":
                    DTMF_symbol = "7";
                    break;
                case "00100100":
                    DTMF_symbol = "8";
                    break;
                case "00100010":
                    DTMF_symbol = "9";
                    break;
                case "00011000":
                    DTMF_symbol = "*";
                    break;
                case "00010100":
                    DTMF_symbol = "0";
                    break;
                case "00010010":
                    DTMF_symbol = "#";
                    break;
                case "10000001":
                    DTMF_symbol = "A";
                    break;
                case "01000001":
                    DTMF_symbol = "B";
                    break;
                case "00100001":
                    DTMF_symbol = "C";
                    break;
                case "00010001":
                    DTMF_symbol = "D";
                    break;
            }

            if (DTMF_symbol != "")//символ определился
            {
                counter = 0; //отсчет завершения слова

                if (last_simbol != DTMF_symbol) //символ сменился
                {
                    DTMF_word = DTMF_word + DTMF_symbol; //символ добавляется к слову
                    last_simbol = DTMF_symbol;
                }

            }

            if (DTMF_word != "") counter++; //у нас есть слово, ждем паузы, когда больше не будут декодироваться символы

            if (counter > 5) //есть пауза
            {                
                textBox1.AppendText(DTMF_word + Environment.NewLine);
                DTMF_word = "";
                last_simbol = "";
                counter = 0;
            }           
          

        }
    }
}
