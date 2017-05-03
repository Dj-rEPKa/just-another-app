using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Threading;

namespace App
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.BackgroundColor = Color.White;
            button1.Text = "Сформировать сетку";
            button2.Text = "Поиск";
            label1.Text = "Колонок:";
            label2.Text = "Строк:";
        }

        private int size_x;
        private int size_y;
        private byte[,] array;
        private DataTable dt;
        private List<Polygon> polygons;
        private List<Polygon> polygons_final;
        private bool stop = false;

        public int Size_X
        {
            get { return size_x; }
            set
            {
                if (value >= 1)
                    size_x = value;
                else
                {
                    textBox1.Text = "1";
                    try
                    {
                        MessageBox.Show(string.Format("Минимально допустимое значение {0} установлено как единица", label1.Text));
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(e1.ToString());
                    }
                }
            }
        }

        public int Size_Y
        {
            get { return size_y; }
            set
            {
                if (value >= 1)
                    size_y = value;
                else
                {
                    textBox2.Text = "1";
                    MessageBox.Show(String.Format("Минимально допустимое значение {0} установлено как единица", label2.Text));
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Size_X = Convert.ToInt16(textBox1.Text);
            Size_Y = Convert.ToInt16(textBox2.Text);
            Array_filling(Size_X, Size_Y);
            polygons = new List<Polygon>();
            polygons_final = new List<Polygon>();
        }

        private void Array_filling(int x, int y)
        {
            array = new byte[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    array[i, j] = 1;
                }
            }

            dt = new DataTable();
            for (int i = 0; i <= array.GetUpperBound(1); i++)
            {
                dt.Columns.Add((i).ToString(), typeof(byte));
            }
            for (int j = 0; j <= array.GetUpperBound(0); j++)
            {
                object[] row = new object[array.GetUpperBound(1) + 1];
                for (int k = 0; k < row.Length; k++)
                {
                    row[k] = array[j, k];
                }
                dt.Rows.Add(row);
            }
            BindingSource srs = new BindingSource { DataSource = dt };
            dataGridView1.DataSource = srs;

        } //filling the array with values and attaching it as datasource to DataGridView

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                pass_through();

                for (int i = 0; i < Size_Y; i++) //changing all cells values from 2 to 1
                {
                    for (int j = 0; j < Size_X; j++)
                    {

                        if ((byte)dataGridView1[i, j].Value == 2)
                        {
                            dataGridView1[i, j].Value = 1;
                        }
                    }
                }

                //setting u color of each poligon
                for (int i = 0; i < polygons_final.Count; i++)
                {
                    polygons_final[i].setColor(generateColor((byte)i));
                }

                string text = $"Всего многоугольников {polygons_final.Count}";
                foreach (Polygon p in polygons_final.OrderByDescending(s => s.Cells.Count))
                {
                    text += $"\n Площадь равна - {p.Cells.Count} клеткам,\n цвет - {p.Color.ToKnownColor()},\n кол-во - { polygons_final.Count(s => s.Cells.Count == p.Cells.Count)}";
                }
                MessageBox.Show(text);
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.ToString());
            }
        }


        private void cell_check(Cell cell, List<Polygon> poly_list, Cell first_in_row, int iterator, int first = 0)
        {
            int i = iterator;
            if (iterator == 2)
                i = iterator;
            if (cell.Column_id < Size_X - 1) 
            {
                Cell temp = new Cell(dataGridView1, cell.Row_id, cell.Column_id + 1);
                poly_list[i].Length++;
                if (temp.Value == 1) //chek cell at right
                {
                    if (first == 0) //just fro first row
                    {
                        poly_list[i].Length_before++; //Length_before is aboslute length of polygon it means dat polyon cant be bigger that it is while Length is cell counter
                    }
                    if (poly_list[i].Length_before >= poly_list[i].Length) //if cell is in limit of polygon lehgth adding this cell to lsit
                    {
                        poly_list[i].Cells.Add(temp);
                    }
                    cell_check(temp, poly_list, first_in_row, i, first);
                    Filtering(poly_list[iterator]); //cutting of unneded cells that could come from adding new Row and the next cell in that row will be unreachable (0 Value)
                    if (stop)
                        return;
                }
                else if (poly_list[i].Length_before > poly_list[i].Length) //decreasing maximum length of polygon
                {
                    poly_list[i].Length_before = poly_list[i].Length;
                    poly_list[i].Length = 1;
                }
            }
            if (first_in_row.Row_id < Size_Y - 1) 
            {
                Cell temp = new Cell(dataGridView1, first_in_row.Row_id + 1, first_in_row.Column_id);
                if (temp.Value == 1)//chek cell in the next row at the same colomn as first cell
                {
                    poly_list[i].Length = 1;
                    poly_list[i].Cells.Add(temp);
                    cell_check(temp, poly_list, temp, i, 1);
                    Filtering(poly_list[iterator]);
                    if (stop)
                        return;
                }
                {
                    
                    stop = true;
                    return;
                }
            }
            else
            {
                stop = true;
                return;
            }
        }//main alghoritm for finding polygons

        private Polygon Filtering(Polygon poly)
        {
            //poly.Cells.RemoveAll(s => s.Column_id > (poly.Cells.Min(x => x.Column_id) + poly.Length_before));
            if (poly.Cells.Count%2 != 0)
            {
                poly.Cells.RemoveAll(s => s.Row_id == (poly.Cells.Max(x => x.Row_id)));
            }
            return poly;
        } //cutting off unneeded cells

        private void Taking_only_right_polygons()
        {
            int i = 0;
            polygons.OrderByDescending(s => s.Cells.Count);
            foreach (Polygon p in polygons.OrderByDescending(s => s.Cells.Count))
            {
                foreach (Cell c in p.Cells.OrderBy(s => s.Column_id).OrderBy(s => s.Row_id))
                {

                    if (c.cell.Style.BackColor == Color.Empty)
                    {
                        c.cell.Value = 2;
                        i++;
                    }
                }
                if (i != 0)
                {
                    polygons_final.Add(p);

                }
                if (polygons.Count > 0)
                {
                    polygons = new List<Polygon>();
                    pass_through();
                    return;
                }
                return;
            }
        } //marking the bigest polygon and then repeating main algh

        private void pass_through()
        {
            for (int i = 0; i < Size_Y; i++)
            {
                for (int j = 0; j < Size_X; j++)
                {
                    var y = dataGridView1[i, j];
                    if ((byte)y.Value == 1)
                    {
                        polygons.Add(new Polygon( polygons.Count));
                        stop = false;
                        Cell cell = new Cell(dataGridView1, y.RowIndex, y.ColumnIndex);
                        polygons[polygons.Count - 1].Cells.Add(cell);
                        cell_check(cell, polygons, cell, polygons.Count - 1);
                    }
                }
            }

            Taking_only_right_polygons();
        } //passing through all cells


        private Color generateColor(byte i)
        {
            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[new Random(i).Next(names.Length)];//we need to use "Seed" for throwing away pseudo-random number issue
            Color randomColor = Color.FromKnownColor(randomColorName);
            var isMenuColor = randomColorName.ToString().Contains("Menu"); //almost all Menu colors are too dark or too bright
            if (randomColor == Color.Black || randomColor == Color.Empty || randomColor == Color.DeepSkyBlue || isMenuColor || randomColor == Color.BlanchedAlmond)
                return generateColor(i);
            else
                return randomColor;
        } //genrating random color


        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridViewCell cell = (DataGridViewCell)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                if (int.Parse(cell.Value.ToString()) == 1)
                {
                    cell.Style.BackColor = Color.Black;
                    cell.Style.ForeColor = Color.White;
                    cell.Value = 0;
                }
                else
                {
                    cell.Value = 1;
                    cell.Style.BackColor = Color.Empty;
                    cell.Style.ForeColor = Color.Black;
                }
            }
            catch { }
        }//changing cell Value and Color on click

        private class Polygon
        {
            private int length;
            private int length_before;
            private Color _color;
            private List<Cell> cell;
            private int increment;

            public Color Color
            {
                get { return _color; }
                set { _color = value; }
            }
            public List<Cell> Cells
            {
                get { return cell; }
                set { cell = value; }
            }
            public int Increment
            {
                get { return increment; }
                set { increment = value; }
            }
            public int Length
            {
                get { return length; }
                set { length = value; }
            }
            public int Length_before
            {
                get { return length_before; }
                set { length_before = value; }
            }

            public Polygon( int increment, int length = 1, int length_before = 1)
            {
                //Color = color;
                Cells = new List<Cell>();
                Increment = increment;
                Length = length;
                Length_before = length_before;
            }

            public void setColor(Color color)
            {
                Color = color;
                foreach (Cell c in cell)
                {
                    c.cell.Style.BackColor = color;
                }
            }
        } //new dataType

        private class Cell
        {
            private int row_id;
            private int column_id;
            private DataGridViewCell _cell;
            private bool empty;
            private byte value;
            private Color _color;
            

            public int Row_id
            {
                get { return row_id; }
                set
                {
                    if (value >= 0)
                        row_id = value;
                    else
                        MessageBox.Show("wrong data");
                }
            }
            public int Column_id
            {
                get { return column_id; }
                set
                {
                    if (value >= 0)
                        column_id = value;
                    else
                        MessageBox.Show("wrong data");
                }
            }
            public DataGridViewCell cell
            {
                get { return _cell; }
                set { _cell = value; }
            }
            public byte Value
            {
                get { return value; }
                set { this.value = value; }
            }
            public bool isEmpty
            {
                get { return empty; }
            }
            public Color Color
            {
                get { return _color; }
                set { _color = value; }
            }
            

            public Cell() : this(new DataGridView(), 0, 0, Color.Empty, true)
            {

            }

            public Cell(DataGridView dg, int Row_ID, int Column_ID) : this(dg, Row_ID, Column_ID, Color.Empty)
            {

            }

            public Cell(DataGridView dg, int Row_ID, int Column_ID, Color color, bool Empty = false)
            {
                empty = Empty;
                Row_id = Row_ID;
                Column_id = Column_ID;
                Color = color;

                
                try
                {
                    cell = dg.Rows[Row_id].Cells[Column_id];
                    Value = byte.Parse(cell.Value.ToString());
                }
                catch 
                {
                    cell = null;
                    Value = 1;
                }
            }

            public override string ToString()
            {
                string myState;
                myState = $"[Column_id: {Column_id}; Row_id: {Row_id}; cell: {cell}; Value: {Value}; isEmpty: {isEmpty};]";
                return myState;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }
        } //another dataType
    }
}
