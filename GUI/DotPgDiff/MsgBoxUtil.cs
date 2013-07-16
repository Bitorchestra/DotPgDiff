using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Modifica los textos en los botones de un MessageBox
/// </summary>
/// <example>
/// MsgBoxUtil.HackMessageBox("Descartar", "Reintentar", "Ignorar");
/// MessageBox.Show("hola3", "hola3", MessageBoxButtons.AbortRetryIgnore);
/// MsgBoxUtil.UnHackMessageBox();</example>
public class MsgBoxUtil
{
    #region Interoperabilidad
    /// <summary>Delegato per passare funzioni con parametro a chiamate della api</summary>
    private delegate bool EnumWindowDelegate(IntPtr handler, IntPtr longPointer);

    /// <summary>Setta il testo di una window</summary>
    [DllImport("user32.dll")]
    private static extern bool SetWindowText(IntPtr handler, string texto);

    /// <summary>Ottiene il nome della classe di una window</summary>
    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr handler, StringBuilder nombre, int tamañoMaximo);

    /// <summary>fa una list delle finestre children e per ogniuna esegue un callback associato</summary>
    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr handler, EnumWindowDelegate callback, IntPtr longPointer);

    /// <summary>fa una list delle finestre children del proc attuale e per ogniuna esegue un callback associato</summary>
    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(int threadID, EnumWindowDelegate callback, IntPtr longPointer);

    /// <summary>Ottiene l'ID del proc attuale</summary>
    [DllImport("kernel32.dll")]
    private static extern int GetCurrentThreadId();

    /// <summary>Ottiene il handler di una window</summary>
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string ClassName, string WindowName);

    /// <summary>Ottiene il thread id del proc dove viene chiamata una finestra</summary>
    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    /// <summary>Realizza una list delle finestre e per ogniuna esegue un callback associato</summary>
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowDelegate lpEnumFunc, IntPtr lParam);

    /// <summary>elimina la referenza ad una finestra</summary>
    [DllImport("user32.dll")]
    static extern bool DestroyWindow(IntPtr hwnd);
    #endregion Interoperabilidad

    #region Oggetti locali
    /// <summary>Array que almacena los textosque se colocaran a cada uno de los botones del msgbox</summary>
    private static string[] textoBotones;
    /// <summary>Indica cual de los botones se esta actualizando</summary>
    private static int indiceTexto;
    /// <summary>Delegado para llamar el proceso de cambio de texto de manera asincrona, se usa en aplicaciones no forms</summary>
    private static WaitCallback EsperarYCambiarMsgBoxWC = new WaitCallback(EsperarYCambiarMsgBox);
    /// <summary>Delegado para buscar e iniciar el proceso de cambio de textos en aplicaciones forms</summary>
    private delegate void BuscarMsgBoxDelegate();
    /// <summary>Handler del ultimo message box modificado</summary>
    private static IntPtr globalHandler;
    #endregion Oggetti locali

    #region Constanti
    /// <summary>Nombre de la clase de la ventana de un messagebox</summary>
    private const string MBOX_CLASSNAME = "#32770";
    /// <summary>Nombre de la clase de la ventana de un boton</summary>
    private const string BUTTON_CLASSNAME = "Button";
    /// <summary>Número de reintentos de hallar un messagebox en el listado de ventanas
    /// , se usa en aplicaciones no forms</summary>
    private const int CICLOS_REINTENTO = 2;
    /// <summary>Tiempo de espera entre reintentos de hallar un messagebox en el listadod de ventanas
    /// , se usa en aplicaciones no forms</summary>
    private const int TIEMPO_REINTENTO = 50;
    /// <summary>Capacidad máxima para StringBuilders</summary>
    private const int STRING_BUILDER_CAPACITY = 256;
    #endregion Constanti

    #region Metodos Publici

    /// <summary>
    /// Modifica el texto de los botones de un messagebox
    /// </summary>
    /// <param name="textoBotones">lista de labels para los botones</param>
    /// <remarks>Internamente se llama a EsperarYCambiarMsgBoxWC
    /// que es un WaitCallBack que llama a EsperarYCambiarMsgBox</remarks>
    public static void HackMessageBox(params string[] textoBotones)
    {
        //guardar referencia global a la lista
        MsgBoxUtil.textoBotones = textoBotones;
        //Si hay al menos una forma... se debe trabajar como aplicación forms
        if (Application.OpenForms.Count > 0)
            //Se llama el delegado desde el hilo actual de forms
            Application.OpenForms[0].BeginInvoke(new BuscarMsgBoxDelegate(BuscaMessageBox));
        else/*sino, se debe presuponer que la instancia del msgbox no ha sido creada
             * y hay que esperar a que se cree*/
            //Llamado asincrono
            ThreadPool.QueueUserWorkItem(EsperarYCambiarMsgBoxWC);
    }

    /// <summary>Destruye el hack aplicado</summary>
    public static void UnHackMessageBox()
    {
        //destruye la ventahna de msgbox a la qu se le aplico el hack
        //windows se encarga de revivir la instancia
        DestroyWindow(MsgBoxUtil.globalHandler);
    }

    #endregion Metodos Publici

    #region Metodos Privati

    /// <summary>Wrapper para simplificar el llamado a EnumThreadWindows desde HackMessageBox</summary>
    private static void BuscaMessageBox()
    {
        EnumThreadWindows(GetCurrentThreadId(), ProcesaMessageBoxEnForms, IntPtr.Zero);
    }

    /// <summary>
    /// Busca las ventanas que sean messagebox y que pertenezcan al proceso actual
    /// sino encuentra ninguna realiza algunos reintentos cada cierto tiempo
    /// </summary>
    /// <param name="stateInfo">no se usa, neceario para crear un waitcallback y asi realizar ejecución desde el threadpool</param>
    /// <remarks>Esta funcion es util cuando no se busca el messagebox desde forms, 
    /// ya que se debe esperar a que la instancia estatica del messagebox sea creada
    /// por lo cual se prevee hacer algunos intentos de busqueda</remarks>
    private static void EsperarYCambiarMsgBox(Object stateInfo)
    {
        int contador = CICLOS_REINTENTO;
        do
        {
            //sino encontro, espera un poco y busca de nuevo
            //EnumWindows retorna true si enumero las ventanas hasta el final
            if (EnumWindows(ProcesaMessageBoxEnOtros, IntPtr.Zero))
                Thread.Sleep(TIEMPO_REINTENTO);
            else//si encontro se sale del ciclo
                break;

        } while (--contador > 0);
    }

    /// <summary>
    /// Determina si el handler pasado es un messagebox, 
    /// si es así inicia el proceso de modificacion de texto de los botones
    /// </summary>
    /// <param name="handler">manejador de la ventana</param>
    /// <param name="longPointer">no se usa, pero se requiere como signature de delegado</param>
    /// <returns>Si el handle pasado no es un MessageBox devuelve true, false de lo contrario</returns>
    private static bool ProcesaMessageBoxEnForms(IntPtr handler, IntPtr longPointer)
    {
        //almacenar nombre de la clase
        StringBuilder nombreClase = new StringBuilder(STRING_BUILDER_CAPACITY);
        GetClassName(handler, nombreClase, nombreClase.Capacity);

        //Si no es un MessageBox...
        if (nombreClase.ToString() != MBOX_CLASSNAME)
            return true;
        else
        {
            //Inicializar indice del arreglo
            indiceTexto = 0;
            //Buscar y cambiar Botones del MessageBox
            EnumChildWindows(handler, CambiaTextoBotonMessageBox, IntPtr.Zero);
            //guardar handler del MessageBox
            MsgBoxUtil.globalHandler = handler;
            return false;
        }
    }

    /// <summary>
    /// Determina si el handler pasado es un messagebox, 
    /// si es así inicia el proceso de modificacion de texto de los botones
    /// </summary>
    /// <param name="handler">manejador de la ventana</param>
    /// <param name="longPointer">no se usa, pero se requiere como signature de delegado</param>
    /// <returns>Si el handle pasado no es un MessageBox devuelve true, false de lo contrario</returns>
    /// <remarks>Util para llamar cuando el proceso que hace el llamado no es forms</remarks>
    private static bool ProcesaMessageBoxEnOtros(IntPtr handler, IntPtr longPointer)
    {
        //almacenar nombre de la clase
        StringBuilder nombreClase = new StringBuilder(STRING_BUILDER_CAPACITY);
        GetClassName(handler, nombreClase, nombreClase.Capacity);

        //Si no es un MessageBox...
        if (nombreClase.ToString() != MBOX_CLASSNAME)
            return true;
        else
        {   //Determinar el id de proceso actual
            int pid = Process.GetCurrentProcess().Id;
            /*Id del proceso de la ventana donde se ejecuta el 
             * MesasgeBox al cual pertenece el handler*/
            int wpid;
            GetWindowThreadProcessId(handler, out wpid);

            //Si son del mismo proceso se procede
            //a realizar la modificacion de textos
            if (wpid == pid)
            {
                //Inicializar indice del arreglo
                indiceTexto = 0;
                //Buscar y cambiar Botones del MessageBox
                EnumChildWindows(handler, CambiaTextoBotonMessageBox, IntPtr.Zero);
                //guardar handler del MessageBox
                MsgBoxUtil.globalHandler = handler;
                return false;
            }
            else
                return true;
        }
    }

    /// <summary>
    /// Cambia el texto del boton recibido como parámetro de acuerdo al listado
    /// de valores iniciales al llamr a HackMessageBox
    /// </summary>
    /// <param name="handler">Manejador del boton</param>
    /// <param name="longPointer">no se usa, pero se requiere como signature de los delegados</param>
    /// <returns>siempre true</returns>
    private static bool CambiaTextoBotonMessageBox(IntPtr handler, IntPtr longPointer)
    {
        //almacenar nombre de la clase
        StringBuilder nombreClase = new StringBuilder(STRING_BUILDER_CAPACITY);
        GetClassName(handler, nombreClase, nombreClase.Capacity);

        //Si el handler es de un boton se modifica el texto
        if (nombreClase.ToString() == BUTTON_CLASSNAME && indiceTexto < textoBotones.Length)
        {
            SetWindowText(handler, textoBotones[indiceTexto]);
            indiceTexto++;
        }
        return true;
    }
    #endregion Metodos Privati
}