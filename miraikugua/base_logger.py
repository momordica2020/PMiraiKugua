import logging
import os

class Logger:
    def __init__(self, name: str, log_file: str = "app.log", level=logging.DEBUG):
        # 创建日志记录器
        self.logger = logging.getLogger(name)
        self.logger.setLevel(level)
        
        # 防止重复添加处理器
        if not self.logger.hasHandlers():
            # 创建格式，包含模块名
            log_format = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(module)s - %(message)s')

            # 创建文件处理器
            file_handler = logging.FileHandler(log_file)
            file_handler.setLevel(level)
            file_handler.setFormatter(log_format)
            self.logger.addHandler(file_handler)

            # 创建控制台处理器
            console_handler = logging.StreamHandler()
            console_handler.setLevel(level)
            console_handler.setFormatter(log_format)
            self.logger.addHandler(console_handler)

    def get_logger(self):
        return self.logger