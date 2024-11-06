# 使用官方Python镜像
FROM python:3.12-slim

# 安装 PDM
RUN pip install pdm

# 设置工作目录
WORKDIR /app

# 复制 pyproject.toml 和 pdm.lock 文件
COPY pyproject.toml pdm.lock* /app/

# 安装项目依赖
RUN pdm install --prod


# 复制当前目录内容到容器中
COPY . .


# 设置容器启动时的命令
CMD ["pdm", "run", "bot_start.py"]
