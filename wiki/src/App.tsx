import { HashRouter, Navigate, Route, Routes, useParams } from 'react-router-dom'
import Sidebar from './components/Sidebar'
import MarkdownPage from './components/MarkdownPage'
import TableOfContents from './components/TableOfContents'
import { pages } from './pages'

function DynamicPage() {
    const { pageId } = useParams<{ pageId: string }>()
    const page = pages.find(p => p.slug === pageId)
    if (!page) return <Navigate to="/" replace />
    return <MarkdownPage slug={page.slug} />
}

export default function App() {
    return (
        <HashRouter>
            <div className="app">
                <Sidebar />
                <main className="content">
                    <Routes>
                        <Route path="/" element={<MarkdownPage slug="Home" />} />
                        <Route path="/:pageId" element={<DynamicPage />} />
                        <Route path="*" element={<Navigate to="/" replace />} />
                    </Routes>
                </main>
                <TableOfContents />
            </div>
        </HashRouter>
    )
}
