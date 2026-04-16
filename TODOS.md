# TODOS

1. Separa o OutboxProcessor, deixar ele especializado para processar imagens, separar a parte que implementar o BackgroundService em outra classe OutboxBackgroundService.

2. Mudar propriedade do OutboxMessages de processed_on_utc para published_on_utc para melhorar o entendimento.

3. Adicionar propriedades no InboxMessage para saber quando uma mensagem foi processada com erro e a razão do erro. Usar o OutboxMessages para se basear nessa implementação.

4. Corrigir estrutura de pastas da library Outbox. Deixar no padrão de projetos e pastas da library Inbox. Separar os projetos de testes do que é abstrações e implementações, igual a library Onbox.