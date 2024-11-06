from graia.ariadne.app import Ariadne

from graia.ariadne.event.message import GroupMessage
from graia.ariadne.event.message import FriendMessage
from graia.ariadne.message.chain import MessageChain

from graia.ariadne.event.mirai import NudgeEvent

from graia.ariadne.model import Friend, Group

from graia.saya import Channel
from graia.saya.builtins.broadcast.schema import ListenerSchema
from graia.saya.event import SayaModuleInstalled

channel = Channel.current()




@channel.use(ListenerSchema(listening_events=[SayaModuleInstalled]))
async def module_listener(event: SayaModuleInstalled):
    print(f"{event.module}::模块加载成功!!!")



@channel.use(ListenerSchema(listening_events=[GroupMessage]))
async def setu(app: Ariadne, group: Group, message: MessageChain):
    if message.display == "我苦签到":
        #  await app.send_message(
        #     group,
        #     MessageChain(f"不要说{message.display}，来点涩图"),
        #  )
         ...


@channel.use(ListenerSchema(listening_events=[NudgeEvent]))
async def getup(app: Ariadne, event: NudgeEvent):
    if isinstance(event.subject, Group) and event.supplicant is not None:
        await app.send_group_message(event.supplicant, MessageChain("你不要光天化日之下在这里戳我啊"))
    elif isinstance(event.subject, Friend) and event.supplicant is not None:
        await app.send_friend_message(event.supplicant, MessageChain("别戳我，好痒！"))