using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace PdfiumViewer
{
    public class PdfAnnotation
    {
        private readonly PdfViewer _pdfViewer;
        private List<PointF> _currentStroke = new List<PointF>();
        private Bitmap _bufferBitmap;
        private Graphics _bufferGraphics;
        PointF prevPoint;

        public PdfAnnotation(PdfViewer pdfViewer)
        {
            _pdfViewer = pdfViewer;

            // 初始化双缓冲
            _pdfViewer.Renderer.Paint += OnRenderPanelPaint;
            _pdfViewer.Renderer.MouseDown += OnMouseDown;
            _pdfViewer.Renderer.MouseMove += OnMouseMove;
            _pdfViewer.Renderer.MouseUp += OnMouseUp;

            _bufferBitmap = new Bitmap(_pdfViewer.Renderer.Width, _pdfViewer.Renderer.Height);
            _bufferGraphics = Graphics.FromImage(_bufferBitmap);
        }

        // 坐标转换：屏幕坐标 → PDF页面坐标
        private PointF ScreenToPdf(Point screenPoint)
        {
            var pdfPoint = _pdfViewer.Renderer.PointToPdf(screenPoint);
            return pdfPoint.Location;
        }

        // 鼠标按下：开始新笔画
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _currentStroke.Clear();
            _currentStroke.Add(ScreenToPdf(e.Location));
        }

        // 鼠标移动：记录点并实时绘制
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _currentStroke.Count == 0) return;

            // 添加新点
            _currentStroke.Add(ScreenToPdf(e.Location));

            // 在缓冲图上绘制线段
            
            if (_currentStroke.Count >= 2)
            {
                using (var pen = new Pen(Color.Red, 5))
                {
                    _bufferGraphics.DrawLine(pen, prevPoint, e.Location);
                }
                    
                ((Control)sender).Invalidate();
            }
            prevPoint = e.Location;
        }

        // 鼠标释放：生成PDF注释
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_currentStroke.Count < 2) return;

            // 创建PDF注释
            _pdfViewer.Document.AddInkAnnotation(_pdfViewer.Renderer.Page, _currentStroke);
            _currentStroke.Clear();

            // 清空缓冲图
            _bufferGraphics.Clear(Color.Transparent);
            ((Control)sender).Invalidate();
        }

        // 渲染双缓冲图
        private void OnRenderPanelPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_bufferBitmap, Point.Empty);
        }

        // 保存PDF
        public void SaveDocument(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                _pdfViewer.Document.Save(stream);
            }
        }
    }
}


