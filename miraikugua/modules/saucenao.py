from graia.ariadne.app import Ariadne
from graia.ariadne.message.element import *
from graia.ariadne.model import Group
from graia.saya import Channel
from arclet.alconna import Args, CommandMeta
from arclet.alconna.graia import Alconna, alcommand, ImgOrUrl, Match
from saucenao_api import AIOSauceNao
from saucenao_api.errors import SauceNaoApiError

channel = Channel.current()

channel.name("Saucenao")
channel.description("以图搜图")
channel.author("RF-Tar-Railt")

apikey = "0a8c227cb0a01639ee3243277e381fb5fb26f2ce" 


search = Alconna(
    "我苦搜图",
    Args["content", ImgOrUrl],
    meta=CommandMeta(
      "以图搜图，搜图结果会自动发送给你",
      usage="你既可以传入图片, 也可以传入图片链接",
      example="我苦搜图 [图片]"
    )
)


@alcommand(search, private=False)
async def saucenao(app: Ariadne, group: Group, source: Source, content: Match[str]):
    if not content.available:
        return await app.send_group_message(group, MessageChain("啊嘞，你传入了个啥子东西"), quote=source.id)
    await app.send_group_message(group, MessageChain("正在搜索，请稍后"), quote=source.id)
    async with AIOSauceNao(apikey, numres=3) as snao:
        try:
            results = await snao.from_url(content.result)
        except SauceNaoApiError as e:
            await app.send_message(group, MessageChain("搜索失败desu"))
            return

    fwd_nodeList = []
    for results in results.results:
        if len(results.urls) == 0:
            continue
        urls = "\n".join(results.urls)
        fwd_nodeList.append(
            ForwardNode(
                target=app.account,
                senderName="爷",
                time=datetime.now(),
                message=MessageChain(
                    f"相似度：{results.similarity}%\n标题：{results.title}\n节点名：{results.index_name}\n链接：{urls}"
                )))

    if len(fwd_nodeList) == 0:
        await app.send_message(group, MessageChain("未找到有价值的数据"), quote=source.id)
    else:
        await app.send_message(group, MessageChain(Forward(nodeList=fwd_nodeList)))