# 使用官方Python镜像
FROM python:3.12-slim

# 安装 Poetry
RUN curl -sSL https://install.python-poetry.org | python3 - && \
    # 将 Poetry 添加到 PATH
    export PATH="/root/.local/bin:$PATH"

WORKDIR /app

# 复制 pyproject.toml 和 poetry.lock 文件
COPY pyproject.toml poetry.lock* /app/

# 安装依赖
RUN poetry config virtualenvs.create false && poetry install --no-root


# 复制当前目录内容到容器中
COPY . .


# 默认运行命令
CMD ["poetry", "run", "python", "bot_start.py"]