#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
市场调研报告PDF生成器
将Markdown格式的市场调研报告转换为专业PDF文档

使用方法：
    python generate_pdf.py --input ./market_research_report.md --output ./市场调研报告.pdf

参数：
    --input: Markdown文件路径（必需）
    --output: 输出PDF文件路径（必需）
"""

import argparse
import re
import os
import sys
from pathlib import Path

# 尝试导入reportlab
try:
    from reportlab.lib.pagesizes import A4
    from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
    from reportlab.lib.units import mm, cm
    from reportlab.lib.colors import HexColor, black, white
    from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether
    from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY, TA_RIGHT
    from reportlab.pdfbase import pdfmetrics
    from reportlab.pdfbase.ttfonts import TTFont
except ImportError:
    print("错误：缺少reportlab库，请安装：pip install reportlab==4.2.0")
    sys.exit(1)


# 颜色定义
PRIMARY_COLOR = HexColor("#1E3A5F")  # 蓝黑色
SECONDARY_COLOR = HexColor("#2C5282")  # 深蓝色
ACCENT_COLOR = HexColor("#3182CE")  # 蓝色
LIGHT_BG_COLOR = HexColor("#F7FAFC")  # 浅灰背景
HEADER_BG_COLOR = HexColor("#2B6CB0")  # 表头背景
ALT_ROW_COLOR = HexColor("#EDF2F7")  # 交替行颜色
TEXT_COLOR = HexColor("#2D3748")  # 正文颜色


def register_chinese_fonts():
    """注册中文字体"""
    # 尝试多个字体路径
    font_paths = [
        # Linux
        "/usr/share/fonts/truetype/wqy/wqy-microhei.ttc",
        "/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc",
        "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
        "/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf",
        # macOS
        "/System/Library/Fonts/PingFang.ttc",
        "/System/Library/Fonts/STHeiti Light.ttc",
        # Windows
        "C:/Windows/Fonts/msyh.ttc",
        "C:/Windows/Fonts/simhei.ttf",
        "C:/Windows/Fonts/simsun.ttc",
    ]
    
    for font_path in font_paths:
        if os.path.exists(font_path):
            try:
                if font_path.endswith('.ttc'):
                    pdfmetrics.registerFont(TTFont('ChineseFont', font_path, subfontIndex=0))
                else:
                    pdfmetrics.registerFont(TTFont('ChineseFont', font_path))
                return 'ChineseFont'
            except Exception:
                continue
    
    # 如果没有找到中文字体，使用内置字体
    return 'Helvetica'


def parse_markdown(content):
    """解析Markdown内容，提取标题、段落、列表、表格"""
    lines = content.strip().split('\n')
    elements = []
    current_section = None
    current_subsection = None
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        
        # 跳过空行
        if not line:
            i += 1
            continue
        
        # 一级标题（# 市场调研报告）
        if line.startswith('# ') and not line.startswith('## '):
            elements.append(('H1', line[2:].strip()))
        
        # 二级标题（## 一、市场概况）
        elif line.startswith('## '):
            elements.append(('H2', line[3:].strip()))
            current_section = line[3:].strip()
        
        # 三级标题（### 2.1 xxx）
        elif line.startswith('### '):
            elements.append(('H3', line[4:].strip()))
            current_subsection = line[4:].strip()
        
        # 列表项（- xxx 或 * xxx）
        elif line.startswith('- ') or line.startswith('* '):
            elements.append(('LIST', line[2:].strip()))
        
        # 有序列表（1. xxx）
        elif re.match(r'^\d+\.\s+', line):
            match = re.match(r'^(\d+)\.\s+(.*)', line)
            if match:
                elements.append(('ORDERED_LIST', match.group(2).strip()))
        
        # 表格开始
        elif line.startswith('|'):
            table_lines = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                table_lines.append(lines[i].strip())
                i += 1
            # 解析表格
            table_data = parse_table(table_lines)
            if table_data:
                elements.append(('TABLE', table_data))
            continue
        
        # 分隔线
        elif line.startswith('---') or line.startswith('***'):
            elements.append(('HR', ''))
        
        # 段落（加粗、斜体、普通文本）
        else:
            elements.append(('PARA', line))
        
        i += 1
    
    return elements


def parse_table(table_lines):
    """解析表格数据"""
    if len(table_lines) < 2:
        return None
    
    rows = []
    for line in table_lines:
        # 跳过分隔行（|---|---|）
        if re.match(r'^\|[\s\-:|]+\|$', line):
            continue
        
        # 解析单元格
        cells = [cell.strip() for cell in line.split('|')[1:-1]]
        rows.append(cells)
    
    if len(rows) < 2:
        return None
    
    return rows


def create_styles(font_name):
    """创建自定义样式"""
    styles = getSampleStyleSheet()
    
    # 报告标题
    styles.add(ParagraphStyle(
        name='ReportTitle',
        parent=styles['Heading1'],
        fontName=font_name,
        fontSize=22,
        leading=30,
        textColor=PRIMARY_COLOR,
        alignment=TA_CENTER,
        spaceAfter=30,
        spaceBefore=20
    ))
    
    # 一级标题
    styles.add(ParagraphStyle(
        name='ChapterTitle',
        parent=styles['Heading1'],
        fontName=font_name,
        fontSize=16,
        leading=24,
        textColor=PRIMARY_COLOR,
        spaceBefore=25,
        spaceAfter=15,
        borderPadding=(5, 5, 5, 5),
    ))
    
    # 二级标题
    styles.add(ParagraphStyle(
        name='SectionTitle',
        parent=styles['Heading2'],
        fontName=font_name,
        fontSize=13,
        leading=20,
        textColor=SECONDARY_COLOR,
        spaceBefore=18,
        spaceAfter=10,
    ))
    
    # 三级标题
    styles.add(ParagraphStyle(
        name='SubSectionTitle',
        parent=styles['Heading3'],
        fontName=font_name,
        fontSize=11,
        leading=16,
        textColor=ACCENT_COLOR,
        spaceBefore=12,
        spaceAfter=8,
    ))
    
    # 正文
    styles.add(ParagraphStyle(
        name='BodyContent',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=10,
        leading=16,
        textColor=TEXT_COLOR,
        alignment=TA_JUSTIFY,
        spaceBefore=6,
        spaceAfter=6,
        firstLineIndent=20,
    ))
    
    # 列表项
    styles.add(ParagraphStyle(
        name='ListItem',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=10,
        leading=15,
        textColor=TEXT_COLOR,
        leftIndent=20,
        spaceBefore=3,
        spaceAfter=3,
        bulletIndent=10,
    ))
    
    # 有序列表
    styles.add(ParagraphStyle(
        name='OrderedListItem',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=10,
        leading=15,
        textColor=TEXT_COLOR,
        leftIndent=25,
        spaceBefore=3,
        spaceAfter=3,
    ))
    
    # 表格表头
    styles.add(ParagraphStyle(
        name='TableHeader',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=9,
        leading=12,
        textColor=white,
        alignment=TA_CENTER,
    ))
    
    # 表格内容
    styles.add(ParagraphStyle(
        name='TableCell',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=9,
        leading=12,
        textColor=TEXT_COLOR,
        alignment=TA_LEFT,
    ))
    
    # 强调文本
    styles.add(ParagraphStyle(
        name='Emphasis',
        parent=styles['Normal'],
        fontName=font_name,
        fontSize=10,
        leading=15,
        textColor=PRIMARY_COLOR,
    ))
    
    return styles


def format_text(text):
    """格式化文本，处理加粗和斜体"""
    # 处理 **加粗**
    text = re.sub(r'\*\*(.*?)\*\*', r'<b>\1</b>', text)
    # 处理 *斜体*
    text = re.sub(r'\*(.*?)\*', r'<i>\1</i>', text)
    # 处理行内代码
    text = re.sub(r'`(.*?)`', r'<font face="Courier">\1</font>', text)
    return text


def build_pdf(elements, styles, output_path):
    """构建PDF文档"""
    doc = SimpleDocTemplate(
        output_path,
        pagesize=A4,
        rightMargin=25*mm,
        leftMargin=25*mm,
        topMargin=25*mm,
        bottomMargin=25*mm,
        title="市场调研报告"
    )
    
    story = []
    
    for elem_type, content in elements:
        if elem_type == 'H1':
            story.append(Paragraph(content, styles['ReportTitle']))
            story.append(Spacer(1, 10))
        
        elif elem_type == 'H2':
            story.append(Spacer(1, 10))
            story.append(Paragraph(content, styles['ChapterTitle']))
        
        elif elem_type == 'H3':
            story.append(Paragraph(content, styles['SectionTitle']))
        
        elif elem_type == 'PARA':
            formatted_text = format_text(content)
            story.append(Paragraph(formatted_text, styles['BodyContent']))
        
        elif elem_type == 'LIST':
            formatted_text = format_text(content)
            story.append(Paragraph(f"• {formatted_text}", styles['ListItem']))
        
        elif elem_type == 'ORDERED_LIST':
            formatted_text = format_text(content)
            story.append(Paragraph(f"1. {formatted_text}", styles['OrderedListItem']))
        
        elif elem_type == 'TABLE':
            table = build_table(content, styles)
            if table:
                story.append(Spacer(1, 10))
                story.append(table)
                story.append(Spacer(1, 10))
        
        elif elem_type == 'HR':
            story.append(Spacer(1, 15))
    
    # 添加页脚
    def add_page_number(canvas, doc):
        canvas.saveState()
        canvas.setFont(styles['Normal'].fontName, 9)
        canvas.setFillColor(HexColor("#718096"))
        page_num = canvas.getPageNumber()
        text = f"- {page_num} -"
        canvas.drawCentredString(A4[0]/2, 15*mm, text)
        canvas.restoreState()
    
    doc.build(story, onFirstPage=add_page_number, onLaterPages=add_page_number)


def build_table(table_data, styles):
    """构建表格"""
    if not table_data or len(table_data) < 2:
        return None
    
    # 分离表头和数据行
    header = table_data[0]
    data_rows = table_data[1:]
    
    # 处理单元格内容
    formatted_header = []
    for cell in header:
        formatted_header.append(Paragraph(cell, styles['TableHeader']))
    
    formatted_data = []
    for row in data_rows:
        formatted_row = []
        for cell in row:
            formatted_row.append(Paragraph(str(cell), styles['TableCell']))
        formatted_data.append(formatted_row)
    
    # 创建表格
    table = Table([formatted_header] + formatted_data, repeatRows=1)
    
    # 设置列宽（平均分配）
    col_count = len(header)
    col_width = (A4[0] - 50*mm) / col_count
    table._argW = [col_width] * col_count
    
    # 表格样式
    style = TableStyle([
        # 表头样式
        ('BACKGROUND', (0, 0), (-1, 0), HEADER_BG_COLOR),
        ('TEXTCOLOR', (0, 0), (-1, 0), white),
        ('ALIGN', (0, 0), (-1, 0), 'CENTER'),
        ('FONTNAME', (0, 0), (-1, 0), styles['TableHeader'].fontName),
        ('FONTSIZE', (0, 0), (-1, 0), 9),
        ('BOTTOMPADDING', (0, 0), (-1, 0), 10),
        ('TOPPADDING', (0, 0), (-1, 0), 10),
        
        # 数据行样式
        ('BACKGROUND', (0, 1), (-1, -1), white),
        ('TEXTCOLOR', (0, 1), (-1, -1), TEXT_COLOR),
        ('ALIGN', (0, 1), (-1, -1), 'LEFT'),
        ('FONTNAME', (0, 1), (-1, -1), styles['TableCell'].fontName),
        ('FONTSIZE', (0, 1), (-1, -1), 9),
        ('TOPPADDING', (0, 1), (-1, -1), 8),
        ('BOTTOMPADDING', (0, 1), (-1, -1), 8),
        
        # 边框
        ('GRID', (0, 0), (-1, -1), 0.5, HexColor("#CBD5E0")),
        ('BOX', (0, 0), (-1, -1), 1, HexColor("#A0AEC0")),
        
        # 对齐
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
    ])
    
    # 交替行颜色
    for i in range(1, len(data_rows) + 1):
        if i % 2 == 0:
            style.add('BACKGROUND', (0, i), (-1, i), ALT_ROW_COLOR)
    
    table.setStyle(style)
    return table


def main():
    parser = argparse.ArgumentParser(
        description='市场调研报告PDF生成器',
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument('--input', '-i', required=True, help='Markdown文件路径')
    parser.add_argument('--output', '-o', required=True, help='输出PDF文件路径')
    
    args = parser.parse_args()
    
    # 检查输入文件
    input_path = Path(args.input)
    if not input_path.exists():
        print(f"错误：输入文件不存在：{args.input}")
        sys.exit(1)
    
    # 注册字体
    font_name = register_chinese_fonts()
    if font_name != 'ChineseFont':
        print(f"警告：未找到中文字体，使用 {font_name}，可能无法正确显示中文")
    
    # 读取Markdown文件
    try:
        with open(input_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"错误：读取文件失败：{e}")
        sys.exit(1)
    
    # 解析Markdown
    print("正在解析Markdown内容...")
    elements = parse_markdown(content)
    
    # 创建样式
    print("正在构建PDF文档...")
    styles = create_styles(font_name)
    
    # 生成PDF
    try:
        build_pdf(elements, styles, args.output)
        print(f"✓ PDF报告已生成：{args.output}")
    except Exception as e:
        print(f"错误：生成PDF失败：{e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
