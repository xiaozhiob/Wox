import ReactDOM from "react-dom/client"
import "./assets/index.css"
import "bootstrap/dist/css/bootstrap.min.css"
import { createBrowserRouter, RouterProvider } from "react-router-dom"
import { WoxMessageHelper } from "./utils/WoxMessageHelper.ts"
import React from "react"
import { invoke } from "@tauri-apps/api/tauri"
import WoxLauncher from "./components/WoxLauncher.tsx"

invoke("get_server_port")
  .then((serverPort) => {
    WoxMessageHelper.getInstance().initialize(serverPort as string)
  })
  .catch(console.log)

const router = createBrowserRouter([
  {
    path: "/",
    element: <WoxLauncher />
  },
  {
    path: "about",
    element: <div>About</div>
  }
])

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
)
