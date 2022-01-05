Module WebService
    Private timeout As Integer = 8 'data transfers time limit (sec)
    Private charEncoder As System.Text.Encoding = System.Text.Encoding.UTF8
    Private serverSocket As System.Net.Sockets.Socket
    Private contentPath As String  'Root path Of web contents

    Public Sub Init(LocalAddress As System.Net.IPAddress)
        '-- A tcp/ip socket (ipv4)
        serverSocket = New System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
        serverSocket.Bind(New System.Net.IPEndPoint(LocalAddress, 15600))
        serverSocket.Listen(5)
        serverSocket.ReceiveTimeout = timeout
        serverSocket.SendTimeout = timeout
    End Sub

End Module
